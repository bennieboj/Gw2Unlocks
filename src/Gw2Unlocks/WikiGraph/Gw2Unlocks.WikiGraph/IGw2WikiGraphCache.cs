using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiGraph
{
    public interface IGw2WikiGraphCache : IGw2WikiGraphSource
    {
        Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken);
    }
}
