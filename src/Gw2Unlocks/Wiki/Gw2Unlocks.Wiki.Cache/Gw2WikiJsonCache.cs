using Gw2Unlocks.Cache.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.Cache;
#pragma warning disable CA1812 // This class is instantiated via DI and not directly, so it may appear unused
internal sealed class Gw2WikiJsonCache : GenericCache, IGw2WikiCache
#pragma warning restore CA1812 // This class is instantiated via DI and not directly, so it may appear unused
{
    private const string unlocksFileName = "unlocks.json";

    public Gw2WikiJsonCache() : base("wiki-cache")
    {
    }

    public Task<ReadOnlyCollection<UnlockInfo>> GetAllUnlocks(ICollection<string> pageTitles, CancellationToken cancellationToken) => LoadFromFileAsync<UnlockInfo>(unlocksFileName, cancellationToken);

    public Task SaveUnlocksToCacheAsync(ReadOnlyCollection<UnlockInfo> data, CancellationToken cancellationToken) => SaveToCacheAsync<UnlockInfo>(unlocksFileName, data, cancellationToken);
}