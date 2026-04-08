using Gw2Unlocks.Cache.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing.Cache;
internal sealed class Gw2WikiGraphJsonCache(CachePaths cachePaths) : GenericCache(cachePaths, "wiki-processing"), IGw2WikiGraphCache
{
    private const string wikiGraphFileName = "wikigraph.json";

    public Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken) => LoadFromFileAsync<AcquisitionGraph>(wikiGraphFileName, cancellationToken);

    public Task<ZoneData> GetZoneData(CancellationToken cancellationToken = default) => LoadFromFileAsync<ZoneData>(wikiGraphFileName, cancellationToken);

    public Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken) => SaveToCacheAsync<AcquisitionGraph>(wikiGraphFileName, data, cancellationToken);
}