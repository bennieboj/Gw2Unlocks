using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing
{
    public interface IGw2WikiGraphCache : IGw2WikiGraphSource
    {
        Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken);
    }
}
