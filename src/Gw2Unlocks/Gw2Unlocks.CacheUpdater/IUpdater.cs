using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

public interface IUpdater
{
    Task UpdateItems(CancellationToken cancellationToken);
}