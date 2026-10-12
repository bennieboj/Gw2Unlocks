using Gw2Unlocks.Cache.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.Cache;

internal sealed class Gw2WikiCache(CachePaths cachePaths)
    : GenericCache(cachePaths.CacheDir, "wiki-cache"), IGw2WikiCache
{
    private const string WikiBulkPrefix = "wikibulk_";
    private const string WikiBulkExtension = ".xml";

    private const long DefaultMaxFileSize = 75 * 1024 * 1024; // 75 MB

    // =============================
    // READ (from multiple files)
    // =============================
    public async IAsyncEnumerable<string> StreamAllPages(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var files = Directory
            .EnumerateFiles(CacheFolder, $"{WikiBulkPrefix}*{WikiBulkExtension}")
            .OrderBy(f => f); // ensure correct order

        foreach (var file in files)
        {
            await foreach (var page in StreamFromFileAsyncEnumerable(
                file,
                ReadXmlPages,
                cancellationToken))
            {
                yield return page;
            }
        }
    }

    public async Task<string?> GetSinglePage(string title, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        await foreach (var page in StreamAllPages(cancellationToken))
        {
            if (page.Contains($"<title>{title}</title>", StringComparison.OrdinalIgnoreCase))
            {
                return page;
            }
        }

        return null;
    }

    // =============================
    // WRITE (split into chunks)
    // =============================
    public async Task StreamPagesToCacheAsync(
        IAsyncEnumerable<string> pages,
        CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(CacheFolder, $"{WikiBulkPrefix}*{WikiBulkExtension}"))
        {
            System.IO.File.Delete(file);
        }

        await StreamPagesToCacheSplitAsync(
            pages,
            DefaultMaxFileSize,
            cancellationToken);
    }

    private async Task StreamPagesToCacheSplitAsync(
        IAsyncEnumerable<string> pages,
        long maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        int fileIndex = 0;
        string currentFileName = "(no file started)";
        int pagesWritten = 0;

        FileStream? stream = null;
        StreamWriter? writer = null;

        async Task CloseCurrentFileAsync()
        {
            if (writer == null)
                return;

            await writer.WriteLineAsync("</mediawiki>");
            await writer.DisposeAsync();
            await stream!.DisposeAsync();

            writer = null;
            stream = null;
        }

        async Task StartNewFileAsync()
        {
            await CloseCurrentFileAsync();

            var fileName = $"{WikiBulkPrefix}{fileIndex++:D4}{WikiBulkExtension}";
            currentFileName = fileName;
            var path = Path.Combine(CacheFolder, fileName);

            stream = System.IO.File.Create(path);
            writer = new StreamWriter(stream);

            await writer.WriteLineAsync(
                @"<?xml version=""1.0"" encoding=""UTF-8""?><mediawiki xmlns=""http://www.mediawiki.org/xml/export-0.11/"">");
        }

        await StartNewFileAsync();

        try
        {
            await foreach (var page in pages.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = writer!.Encoding.GetByteCount(page + Environment.NewLine);

                if (stream!.Length + bytes > maxFileSizeBytes)
                {
                    await StartNewFileAsync();
                }

                await writer.WriteLineAsync(page);
                pagesWritten++;
            }
        }
        catch (OperationCanceledException)
        {
            // Keep cancellation distinguishable for callers (e.g. UpdaterService shutdown handling).
            throw;
        }
        catch (Exception ex)
        {
            // Surface which cache file was being written when the page source failed.
            throw new InvalidOperationException(
                $"Wiki page streaming failed while writing '{currentFileName}' " +
                $"({pagesWritten} page(s) written so far). The failure is in the page source. See inner exception for details.",
                ex);
        }

        await CloseCurrentFileAsync();
    }
}