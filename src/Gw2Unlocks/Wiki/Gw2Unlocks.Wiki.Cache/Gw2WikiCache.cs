using Gw2Unlocks.Cache.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.Cache;

internal sealed class Gw2WikiCache(CachePaths cachePaths)
    : GenericCache(cachePaths, "wiki-cache"), IGw2WikiCache
{
    private const string WikiBulkPrefix = "wikibulk_";
    private const string WikiBulkExtension = ".xml";

    private const long DefaultMaxFileSize = 75 * 1024 * 1024; // 75 MB

    // =============================
    // READ (from multiple files)
    // =============================
    public async IAsyncEnumerable<string> StreamAllPages(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
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

    // =============================
    // WRITE (split into chunks)
    // =============================
    public async Task StreamPagesToCacheAsync(
        IAsyncEnumerable<string> pages,
        CancellationToken cancellationToken = default)
    {
        foreach (var file in Directory.EnumerateFiles(CacheFolder, $"{WikiBulkPrefix}*{WikiBulkExtension}"))
        {
            File.Delete(file);
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
            var path = Path.Combine(CacheFolder, fileName);

            stream = File.Create(path);
            writer = new StreamWriter(stream);

            await writer.WriteLineAsync(
                @"<?xml version=""1.0"" encoding=""UTF-8""?><mediawiki xmlns=""http://www.mediawiki.org/xml/export-0.11/"">");
        }

        await StartNewFileAsync();

        await foreach (var page in pages.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytes = writer!.Encoding.GetByteCount(page + Environment.NewLine);

            if (stream!.Length + bytes > maxFileSizeBytes)
            {
                await StartNewFileAsync();
            }

            await writer.WriteLineAsync(page);
        }

        await CloseCurrentFileAsync();
    }
}