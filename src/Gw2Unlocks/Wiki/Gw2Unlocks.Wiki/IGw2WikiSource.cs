using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki;

public interface IGw2WikiSource
{
    Task<AcquisitionGraph> GetAcquisitionGraph(IEnumerable<string> itemNames, AcquisitionGraph? existingGraph = null, CancellationToken cancellationToken = default);
}
