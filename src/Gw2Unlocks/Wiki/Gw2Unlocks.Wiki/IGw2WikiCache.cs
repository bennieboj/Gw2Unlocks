using Gw2Unlocks.Cache.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki
{
    public interface IGw2WikiCache : IGw2WikiSource
    {
        Task SaveUnlocksToCacheAsync(ReadOnlyCollection<UnlockInfo> data, CancellationToken cancellationToken);
    }
}
