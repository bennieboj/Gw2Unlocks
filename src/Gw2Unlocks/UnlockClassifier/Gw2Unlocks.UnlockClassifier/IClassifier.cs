using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

public interface IClassifier
{
    Task ClassifyUnlocks(CancellationToken cancellationToken);
    Task<string> ClassifyUnlock(string unlock, CancellationToken cancellationToken);
}