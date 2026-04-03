using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki
{
    public interface IGw2WikiCache : IGw2WikiSource
    {
        Task SaveAllPagesToCacheAsync(Collection<string> data, CancellationToken cancellationToken);
        Task StreamPagesToCacheAsync(IAsyncEnumerable<string> pages, CancellationToken cancellationToken = default);
    }
}
