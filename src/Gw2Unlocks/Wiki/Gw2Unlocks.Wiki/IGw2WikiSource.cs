using System.Collections.Generic;
using System.Threading;

namespace Gw2Unlocks.Wiki;

public interface IGw2WikiSource
{
    IAsyncEnumerable<string> StreamAllPages(CancellationToken cancellationToken = default);
}
