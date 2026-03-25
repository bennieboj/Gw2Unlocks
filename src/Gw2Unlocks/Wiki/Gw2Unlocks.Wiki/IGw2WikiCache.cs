using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki
{
    public interface IGw2WikiCache : IGw2WikiSource
    {
        Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken);
    }
}
