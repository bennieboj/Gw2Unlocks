using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    public async IAsyncEnumerable<string> StreamAllPages(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var site = await lazySite.Value;
        var generator = new AllPagesGenerator(site) { NamespaceId = 0, PaginationSize = 100 };
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        var batch = new List<string>();
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        int totalPages = 0;

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

            logger.LogInformation("Exported {Count} pages total", totalPages);
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

    public async Task<Collection<string>> GetAllPages(CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 5, // number of retries
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // exponential backoff
                onRetry: (response, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Request failed with {response.Result.StatusCode}. Waiting {timespan} before retry {retryCount}.");
                });

        var batch = new List<string>();
        int batchSize = 100;

        var site = await lazySite.Value;
        var generator = new AllPagesGenerator(site)
        {
            NamespaceId = 0, // 0 = main
            PaginationSize = batchSize
        };

        var xmls = new Collection<string>();
        await foreach (var page in generator.EnumPagesAsync())
        {
            //if (xmls.Count > 5) break;

            if (page?.Title == null) continue;

            batch.Add(page.Title);

            if (batch.Count >= batchSize)
            {
                xmls.Add(await DownloadExportBatch(http, retryPolicy, batch, cancellationToken));
                batch.Clear();
                logger.LogInformation("exported {count}", xmls.Count * batchSize);
                await Task.Delay(100, cancellationToken); // be nice to the server
            }

        }

        // remaining pages
        if (batch.Count > 0)
        {
            xmls.Add(await DownloadExportBatch(http, retryPolicy, batch, cancellationToken));
            logger.LogInformation("exported {count}", xmls.Count * batchSize + batch.Count);
        }
        return xmls;
    }

    private static async Task<string> DownloadExportBatch(
    HttpClient http,
    AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
    List<string> titles,
    CancellationToken cancellationToken)
    {
        var url = new Uri("https://wiki.guildwars2.com/wiki/Special:Export");
        var sb = new StringBuilder();
        sb.Append("title=Special%3AExport&pages=");
        for (int i = 0; i < titles.Count; i++)
        {
            if (i > 0) sb.Append("%0D%0A");
            sb.Append(Uri.EscapeDataString(titles[i]).Replace("%20", "+", StringComparison.Ordinal));
        }
        sb.Append("&curonly=1&wpDownload=1");

        using var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await retryPolicy.ExecuteAsync(() => http.PostAsync(url, content, cancellationToken));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public Task<string?> GetSinglePage(string title, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}