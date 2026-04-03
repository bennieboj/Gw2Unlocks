using Gw2Unlocks.Cache.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.Cache;
internal sealed class Gw2WikiCache(CachePaths cachePaths) : GenericCache(cachePaths, "wiki-cache"), IGw2WikiCache
{
    private const string wikiBulkFileName = "wikibulk.xml";

    public Task<Collection<string>> GetAllPages(CancellationToken cancellationToken = default) => LoadFromFileAsync<Collection<string>>(wikiBulkFileName, cancellationToken);

    public Task SaveAllPagesToCacheAsync(Collection<string> data, CancellationToken cancellationToken) => SaveToCacheAsync<Collection<string>>(wikiBulkFileName, data, cancellationToken);

    public IAsyncEnumerable<string> StreamAllPages(CancellationToken cancellationToken = default)
    {
        return StreamFromFileAsyncEnumerable(
            wikiBulkFileName,
            ReadXmlPages,
            cancellationToken);
    }

    public async Task StreamPagesToCacheAsync(
    IAsyncEnumerable<string> pages,
    CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(Path.Combine(CacheFolder, wikiBulkFileName));
        await using var writer = new StreamWriter(stream);

        // optional XML header
        await writer.WriteLineAsync(@"<?xml version=""1.0"" encoding=""UTF-8""?><mediawiki xmlns=""http://www.mediawiki.org/xml/export-0.11/"">");

        await WritePagesAsync(pages, writer, cancellationToken);

        await writer.WriteLineAsync("</mediawiki>");
    }
}