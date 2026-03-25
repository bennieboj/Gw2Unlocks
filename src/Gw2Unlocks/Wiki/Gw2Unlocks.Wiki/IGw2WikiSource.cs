using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki;

public interface IGw2WikiSource
{
    Task<ReadOnlyCollection<UnlockInfo>> GetAllUnlocks(ICollection<string> pageTitles, CancellationToken cancellationToken);
}
