using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing
{
    public interface IGw2WikiProcessingCache : IGw2WikiProcessingSource
    {
        Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken);
        Task SaveZoneDataToCacheAsync(ZoneData data, CancellationToken cancellationToken);
    }
}
