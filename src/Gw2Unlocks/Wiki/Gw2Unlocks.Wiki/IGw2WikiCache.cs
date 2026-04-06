using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki
{
    public interface IGw2WikiCache : IGw2WikiSource
    {
        Task StreamPagesToCacheAsync(IAsyncEnumerable<string> pages, CancellationToken cancellationToken = default);
    }
}
