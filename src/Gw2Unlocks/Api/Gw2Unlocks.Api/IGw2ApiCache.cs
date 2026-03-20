using GuildWars2.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api;

public interface IGw2ApiCache : IGw2ApiSource
{
    Task SaveToCacheAsync<T>(string fileName, IImmutableValueSet<T> data, CancellationToken cancellationToken);
}
