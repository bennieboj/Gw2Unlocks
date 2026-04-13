using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing;

public interface IGw2WikiProcessingSource
{
    Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken);
    Task<ZoneData> GetZoneData(CancellationToken cancellationToken);
}
