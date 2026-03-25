using Gw2Unlocks.Api;
using Gw2Unlocks.Wiki;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal class Updater(IGw2ApiSource apiSource, IGw2ApiCache apiCache, IGw2WikiSource wikiSource, IGw2WikiCache wikiCache) : IUpdater
{
    private const int MaxRetries = 5;

    public async Task UpdateApiData(CancellationToken cancellationToken)
    {
        // Items
        var items = await RetryAsync(() => apiSource.GetItemsAsync(cancellationToken), "Items");
        await apiCache.SaveItemsToCacheAsync(items, cancellationToken);

        // Achievements
        var achievements = await RetryAsync(() => apiSource.GetAchievementsAsync(cancellationToken), "Achievements");
        await apiCache.SaveAchievementsToCacheAsync(achievements, cancellationToken);

        // Miniatures
        var miniatures = await RetryAsync(() => apiSource.GetMiniaturesAsync(cancellationToken), "Miniatures");
        await apiCache.SaveMiniaturesToCacheAsync(miniatures, cancellationToken);

        // Novelties
        var novelties = await RetryAsync(() => apiSource.GetNoveltiesAsync(cancellationToken), "Novelties");
        await apiCache.SaveNoveltiesToCacheAsync(novelties, cancellationToken);

        // Titles
        var titles = await RetryAsync(() => apiSource.GetTitlesAsync(cancellationToken), "Titles");
        await apiCache.SaveTitlesToCacheAsync(titles, cancellationToken);
    }

    private static async Task<ReadOnlyCollection<T>> RetryAsync<T>(Func<Task<ReadOnlyCollection<T>>> action, string name)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                return await action();
            }
            catch (Exception ex) when (attempt <= MaxRetries)
            {
                Console.WriteLine($"Attempt {attempt} for {name} failed: {ex.Message}. Retrying...");
                //await Task.Delay(200 * attempt); // optional backoff
            }
        }
    }

    public async Task UpdateWikiData(CancellationToken cancellationToken)
    {
        var minis = await apiCache.GetMiniaturesAsync(cancellationToken);

        var results = new List<UnlockInfo> (await wikiCache.GetAllUnlocks([], CancellationToken.None));
        var existingNames = results.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var remaining = minis
            .Where(m => !existingNames.Contains(m.Name))
            .ToList();

        foreach (var chunk in remaining.Chunk(20))
        {
            try
            {
                var data = await wikiSource.GetAllUnlocks([.. chunk.Select(m => m.Name)], cancellationToken);
                results.AddRange(data);
            }
            catch (OperationCanceledException)
            {
                await wikiCache.SaveUnlocksToCacheAsync(new ReadOnlyCollection<UnlockInfo>(results), CancellationToken.None);
                throw;
            }

            await wikiCache.SaveUnlocksToCacheAsync(new ReadOnlyCollection<UnlockInfo>(results), CancellationToken.None);
        }
    }
}