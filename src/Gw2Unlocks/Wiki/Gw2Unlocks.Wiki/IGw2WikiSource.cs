using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki;

public interface IGw2WikiSource
{
    IAsyncEnumerable<string> StreamAllPages(CancellationToken cancellationToken = default);

    Task<string?> GetSinglePage(string title, CancellationToken cancellationToken = default);
}
