using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier
{
    internal interface IClassifierCache
    {
        Task SaveClassifierConfigToCacheAsync(ClassifyConfig data, CancellationToken cancellationToken);
    }
}
