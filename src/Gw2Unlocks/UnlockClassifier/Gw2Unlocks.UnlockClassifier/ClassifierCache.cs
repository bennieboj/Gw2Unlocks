using Gw2Unlocks.Cache.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier
{
    internal class ClassifierCache(CachePaths cachePaths) : GenericCache(cachePaths, "../Gw2Unlocks/site/public/data"), IClassifierCache
    {
        private const string classifierConfigFileName = "classifier-config.json";
        public Task<ClassifyConfig> GetClassifierConfigFromCacheAsync(CancellationToken cancellationToken) => LoadFromFileAsync<ClassifyConfig>(classifierConfigFileName, cancellationToken);
        public Task SaveClassifierConfigToCacheAsync(ClassifyConfig data, CancellationToken cancellationToken) => SaveToCacheAsync(classifierConfigFileName, data, cancellationToken);
    }
}
