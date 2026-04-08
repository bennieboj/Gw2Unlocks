using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Wiki;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing.IntegrationTests
{
    internal sealed class Gw2WikiIntegrationTestSuccessResponseFake(CachePaths cachepaths) : GenericCache(cachepaths, "."), IGw2WikiSource, IGw2WikiCache
    {
        internal required string FileName { get; set; } = string.Empty;

        public Task<Collection<string>> GetAllPages(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string?> GetSinglePage(string title, CancellationToken cancellationToken = default)
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

        public Task SaveAllPagesToCacheAsync(Collection<string> data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<string> StreamAllPages(CancellationToken cancellationToken = default)
        {
            return StreamFromFileAsyncEnumerable(
                FileName,
                ReadXmlPages,
                cancellationToken);
        }

        public Task StreamPagesToCacheAsync(IAsyncEnumerable<string> pages, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}