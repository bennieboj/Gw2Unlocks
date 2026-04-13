using Gw2Unlocks.Cache.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier
{
    internal class ClassifierCache(CachePaths cachePaths) : GenericCache(cachePaths, "classifier-cache"), IClassifierCache
    {
        private const string classifierConfigFileName = "classifier-config.json";
        public Task SaveClassifierConfigToCacheAsync(ClassifyConfig data, CancellationToken cancellationToken) => SaveToCacheAsync(classifierConfigFileName, data, cancellationToken);
    }
}
