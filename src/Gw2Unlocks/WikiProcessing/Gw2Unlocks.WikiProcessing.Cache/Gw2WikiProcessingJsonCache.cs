using Gw2Unlocks.Cache.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing.Cache;
internal sealed class Gw2WikiProcessingJsonCache(CachePaths cachePaths) : GenericCache(cachePaths, "wiki-processing"), IGw2WikiProcessingCache
{
    private const string wikiGraphFileName = "wikigraph.json";
    private const string zoneDataFileName = "zonedata.json";

    private static Lazy<Task<AcquisitionGraph>>? _graph;
    private static Lazy<Task<ZoneData>>? _zoneData;

    public Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken)
        => (_graph ??= new Lazy<Task<AcquisitionGraph>>(() =>
            LoadFromFileAsync<AcquisitionGraph>(wikiGraphFileName, cancellationToken)))
        .Value;

    public Task<ZoneData> GetZoneData(CancellationToken cancellationToken)
        => (_zoneData ??= new Lazy<Task<ZoneData>>(() =>
            LoadFromFileAsync<ZoneData>(zoneDataFileName, cancellationToken)))
        .Value;

    public Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken) => SaveToCacheAsync(wikiGraphFileName, data, cancellationToken);
    public Task SaveZoneDataToCacheAsync(ZoneData data, CancellationToken cancellationToken) => SaveToCacheAsync(zoneDataFileName, data, cancellationToken);
}