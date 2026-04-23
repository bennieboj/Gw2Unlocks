using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

public interface IClassifier
{
    Task<ClassifyConfig> ClassifyUnlocks(CancellationToken cancellationToken, params string[] unlocksToLookup);
}