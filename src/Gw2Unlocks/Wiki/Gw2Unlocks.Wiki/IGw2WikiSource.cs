using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki;

public interface IGw2WikiSource
{
    Task<Collection<string>> GetAllPages(CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAllPages(CancellationToken cancellationToken = default);
}
