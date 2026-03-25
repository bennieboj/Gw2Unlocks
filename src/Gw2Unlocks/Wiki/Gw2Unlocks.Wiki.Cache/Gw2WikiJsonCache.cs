using Gw2Unlocks.Cache.Common;
using System.Collections.Generic;
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

    public Task<AcquisitionGraph> GetAcquisitionGraph(IEnumerable<string> itemNames, AcquisitionGraph? graph, CancellationToken cancellationToken) => LoadFromFileAsync<AcquisitionGraph>(unlocksFileName, cancellationToken);

    public Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken) => SaveToCacheAsync<AcquisitionGraph>(unlocksFileName, data, cancellationToken);
}