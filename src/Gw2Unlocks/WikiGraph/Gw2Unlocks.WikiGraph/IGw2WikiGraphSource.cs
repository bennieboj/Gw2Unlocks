using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiGraph;

public interface IGw2WikiGraphSource
{
    Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken = default);
}
