using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier
{
    internal interface IClassifierCache
    {
        Task<ClassifyConfig> GetClassifierConfigFromCacheAsync(CancellationToken cancellationToken);
        Task SaveClassifierConfigToCacheAsync(ClassifyConfig data, CancellationToken cancellationToken);
    }
}
