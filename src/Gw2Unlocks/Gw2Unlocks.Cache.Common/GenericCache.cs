using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Cache.Common
{
    public class GenericCache
    {
        private readonly string cacheFolder;

        public GenericCache(string cacheFolder)
        {
            this.cacheFolder = Path.Combine(CachePaths.Root, cacheFolder);
            Directory.CreateDirectory(this.cacheFolder);
        }

        // --- Generic helper to read JSON ---
        protected async Task<T> LoadFromFileAsync<T>(string fileName, CancellationToken cancellationToken)
        {
            var path = Path.Combine(CachePaths.Root, cacheFolder, fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Cache file not found: {path}");

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<T>(json)!;
        }

        // --- Generic helper to save JSON from caller-provided data ---
        public async Task SaveToCacheAsync<T>(string fileName, T data, CancellationToken cancellationToken)
        {
            var path = Path.Combine(CachePaths.Root, cacheFolder, fileName);
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
