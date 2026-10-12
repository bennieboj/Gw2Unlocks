using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WikiClientLibrary.Sites;

namespace Gw2Unlocks.Wiki.Implementation;

public sealed class Gw2WikiSource(ILogger<Gw2WikiSource> logger, Lazy<Task<WikiSite>> lazySite) : IGw2WikiSource
{
    private const string ExportEndpoint = "https://wiki.guildwars2.com/wiki/Special:Export";

    // MediaWiki max page size for list=allpages without a bot account.
    private const int ListingPageSize = 500;

    private const int ExportBatchSize = 100;

    // wiki.guildwars2.com bans sustained request bursts from runner IPs (plain nginx 403s);
    // the pause after every listing fetch and export batch keeps the crawl under the rate
    // that triggers that. Single shared throttle point for both request streams.
    private static readonly TimeSpan BatchPause = TimeSpan.FromSeconds(2);

    public async IAsyncEnumerable<string> StreamAllPages(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Initialization gate: proves the wiki is reachable before crawling.
        await lazySite.Value;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(ServiceCollectionExtensions.UserAgent);

        // Retries ride out the same 403 windows as the siteinfo handshake. The ladder is long
        // on purpose: a failure that gives up early throws away every page streamed so far,
        // because the cache writer restarts from the first file on the next run.
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 5,
                // 15s, 30s, 60s, 120s, 240s
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(15 * Math.Pow(2, attempt - 1)),
                onRetry: (result, delay, attempt, _) => logger.LogWarning(
                    "Wiki request returned {StatusCode}; retry {Attempt}/5 in {DelaySeconds}s",
                    (int)result.Result!.StatusCode, attempt, delay.TotalSeconds));

        int totalPages = 0;

        // Search here to find the namepace ids: https://wiki.guildwars2.com/wiki/Special:AllPages
        foreach (var namespaceId in new[] { 0, 10 }) // Main, Templates
        {
            var batch = new List<string>();

            // Own listing loop instead of AllPagesGenerator.EnumPagesAsync(): that library call
            // issues api.php requests outside our throttle/retry path, which is where the
            // mid-crawl 403s came from. Listing here goes through the same HttpClient,
            // retry ladder and BatchPause as the export POSTs.
            await foreach (var title in ListAllTitles(http, retryPolicy, namespaceId, cancellationToken))
            {
                batch.Add(title);

                if (batch.Count >= ExportBatchSize)
                {
                    await foreach (var pageXml in ProcessBatch(batch, http, retryPolicy, cancellationToken).WithCancellation(cancellationToken))
                    {
                        totalPages++;
                        yield return pageXml;
                    }

                    logger.LogInformation("Exported {Count} pages so far", totalPages);

                    await Task.Delay(BatchPause, cancellationToken);

                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await foreach (var pageXml in ProcessBatch(batch, http, retryPolicy, cancellationToken).WithCancellation(cancellationToken))
                {
                    totalPages++;
                    yield return pageXml;
                }

                logger.LogInformation("Exported {Count} pages so far", totalPages);

                await Task.Delay(BatchPause, cancellationToken);
            }
        }

        logger.LogInformation("Export complete. Exported {Count} pages total", totalPages);
    }

    private static async IAsyncEnumerable<string> ListAllTitles(
        HttpClient http,
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
        int namespaceId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Direct list=allpages pagination: action=query&list=allpages&apnamespace=<id>
        // &aplimit=500&format=json&formatversion=2, following query-continue.apcontinue.
        // apfrom (= last title) doubles as the resume point: a retried fetch restarts
        // from the same apfrom instead of the namespace start, so a 403 window only
        // replays one listing page, not the whole crawl.
        string? apfrom = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = new StringBuilder(ServiceCollectionExtensions.WikiApiEndpoint)
                .Append("?action=query&list=allpages")
                .Append("&apnamespace=").Append(namespaceId)
                .Append("&aplimit=").Append(ListingPageSize)
                .Append("&format=json&formatversion=2");
            if (apfrom is not null)
                url.Append("&apfrom=").Append(Uri.EscapeDataString(apfrom));

            using var response = await retryPolicy.ExecuteAsync(() => http.GetAsync(new Uri(url.ToString()), cancellationToken));
            if (!response.IsSuccessStatusCode)
            {
                var bodySnippet = await response.Content.ReadAsStringAsync(cancellationToken);
                if (bodySnippet.Length > 300)
                    bodySnippet = bodySnippet[..300];

                throw new HttpRequestException(
                    $"GET '{ServiceCollectionExtensions.WikiApiEndpoint}' (list=allpages, namespace {namespaceId}, apfrom '{apfrom ?? "<start>"}') " +
                    $"returned {(int)response.StatusCode} ({response.StatusCode}) after retries. " +
                    $"Response body starts with: {bodySnippet}",
                    null,
                    response.StatusCode);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            string? lastTitle = null;
            if (doc.RootElement.TryGetProperty("query", out var query) &&
                query.TryGetProperty("allpages", out var pages))
            {
                foreach (var page in pages.EnumerateArray())
                {
                    if (page.TryGetProperty("title", out var titleProp) &&
                        titleProp.GetString() is string title)
                    {
                        lastTitle = title;
                        yield return title;
                    }
                }
            }

            string? apcontinue = null;
            if (doc.RootElement.TryGetProperty("continue", out var cont) &&
                cont.TryGetProperty("apcontinue", out var apcontProp))
                apcontinue = apcontProp.GetString();

            // No continuation: namespace exhausted.
            if (apcontinue is null)
                break;

            // Resume inside the page if the response was cut mid-title-list:
            // apcontinue is the title to continue from.
            apfrom = apcontinue;

            // MediaWiki answered with an error payload instead of a result set.
            if (lastTitle is null && !doc.RootElement.TryGetProperty("query", out _))
            {
                var errorInfo = doc.RootElement.TryGetProperty("error", out var err)
                    ? err.GetRawText()
                    : doc.RootElement.GetRawText()[..Math.Min(300, doc.RootElement.GetRawText().Length)];
                throw new HttpRequestException(
                    $"GET '{ServiceCollectionExtensions.WikiApiEndpoint}' (list=allpages, namespace {namespaceId}) " +
                    $"returned an API error payload after retries: {errorInfo}");
            }

            await Task.Delay(BatchPause, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<string> ProcessBatch(
        List<string> titles,
        HttpClient http,
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Download the batch as XML
        var xml = await DownloadExportBatch(http, retryPolicy, titles, cancellationToken);

        // Parse pages from the XML stream
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, new XmlReaderSettings { Async = true });

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "page")
                yield return await reader.ReadOuterXmlAsync();
        }
    }


    private static async Task<string> DownloadExportBatch(
    HttpClient http,
    AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
    List<string> titles,
    CancellationToken cancellationToken)
    {
        var url = new Uri(ExportEndpoint);
        var sb = new StringBuilder();
        sb.Append("title=Special%3AExport&pages=");
        for (int i = 0; i < titles.Count; i++)
        {
            if (i > 0) sb.Append("%0D%0A");
            sb.Append(Uri.EscapeDataString(titles[i]).Replace("%20", "+", StringComparison.Ordinal));
        }
        sb.Append("&curonly=1&wpDownload=1");

        using var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        using var response = await retryPolicy.ExecuteAsync(() => http.PostAsync(url, content, cancellationToken));
        if (!response.IsSuccessStatusCode)
        {
            // The bare EnsureSuccessStatusCode() exception loses the URL and batch,
            // so include both to make the failure reproducible locally.
            var bodySnippet = await response.Content.ReadAsStringAsync(cancellationToken);
            if (bodySnippet.Length > 300)
                bodySnippet = bodySnippet[..300];

            var batchSample = string.Join(", ", titles.Take(5));
            if (titles.Count > 5)
                batchSample += $", ... (+{titles.Count - 5} more)";

            throw new HttpRequestException(
                $"POST '{url}' returned {(int)response.StatusCode} ({response.StatusCode}) after retries. " +
                $"Batch of {titles.Count} page(s): {batchSample}. " +
                $"Response body starts with: {bodySnippet}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public Task<string?> GetSinglePage(string title, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}