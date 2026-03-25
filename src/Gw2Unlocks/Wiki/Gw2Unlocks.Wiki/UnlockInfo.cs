using System.Collections.Generic;

namespace Gw2Unlocks.Wiki;

public record UnlockInfo(
    string Name,
    IReadOnlyList<IReadOnlyList<AcquisitionNode>> Paths
);