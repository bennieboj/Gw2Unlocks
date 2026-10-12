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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Sites;

namespace Gw2Unlocks.Wiki.Implementation;

public sealed class Gw2WikiSource(ILogger<Gw2WikiSource> logger, Lazy<Task<WikiSite>> lazySite) : IGw2WikiSource
{
    private const string ExportEndpoint = "https://wiki.guildwars2.com/wiki/Special:Export";

    // wiki.guildwars2.com bans sustained request bursts from runner IPs (plain nginx 403s);
    // the pause after every export batch keeps the crawl under the rate that triggers that.
    private static readonly TimeSpan BatchPause = TimeSpan.FromSeconds(2);

    public async IAsyncEnumerable<string> StreamAllPages(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var site = await lazySite.Value;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(ServiceCollectionExtensions.UserAgent);

        // Retries ride out the same 403 windows as the siteinfo handshake. The ladder is long
        // on purpose: a batch that gives up early throws away every page streamed so far,
        // because the cache writer restarts from the first file on the next run.
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 5,
                // 15s, 30s, 60s, 120s, 240s
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(15 * Math.Pow(2, attempt - 1)),
                onRetry: (result, delay, attempt, _) => logger.LogWarning(
                    "Export POST to {ExportEndpoint} returned {StatusCode}; retry {Attempt}/5 in {DelaySeconds}s",
                    ExportEndpoint, (int)result.Result!.StatusCode, attempt, delay.TotalSeconds));

        int totalPages = 0;

        // Search here to find the namepace ids: https://wiki.guildwars2.com/wiki/Special:AllPages
        foreach (var namespaceId in new[] { 0, 10 }) // Main, Templates
        {
            var generator = new AllPagesGenerator(site) { NamespaceId = namespaceId, PaginationSize = 100 };

            var batch = new List<string>();

            await foreach (var page in generator.EnumPagesAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (page?.Title == null) continue;

                batch.Add(page.Title);

                if (batch.Count >= 100)
                {
                    int batchCount = 0;

                    await foreach (var pageXml in ProcessBatch(batch, http, retryPolicy, cancellationToken))
                    {
                        batchCount++;
                        yield return pageXml;
                    }

                    totalPages += batchCount;

                    logger.LogInformation("Exported {Count} pages so far", totalPages);

                    await Task.Delay(BatchPause, cancellationToken);

                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                int batchCount = 0;

                await foreach (var pageXml in ProcessBatch(batch, http, retryPolicy, cancellationToken))
                {
                    batchCount++;
                    yield return pageXml;
                }

                totalPages += batchCount;

                logger.LogInformation("Exported {Count} pages so far", totalPages);

                await Task.Delay(BatchPause, cancellationToken);
            }
        }

        logger.LogInformation("Export complete. Exported {Count} pages total", totalPages);
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