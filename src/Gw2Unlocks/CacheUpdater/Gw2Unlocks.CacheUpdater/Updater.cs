using Gw2Unlocks.Api;
using Gw2Unlocks.Wiki;
using System;
using System.Collections.ObjectModel;
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
        await apiCache.SaveToCacheAsync("items.json", items, cancellationToken);

        // Achievements
        var achievements = await RetryAsync(() => apiSource.GetAchievementsAsync(cancellationToken), "Achievements");
        await apiCache.SaveToCacheAsync("achievements.json", achievements, cancellationToken);

        // Miniatures
        var miniatures = await RetryAsync(() => apiSource.GetMiniaturesAsync(cancellationToken), "Miniatures");
        await apiCache.SaveToCacheAsync("miniatures.json", miniatures, cancellationToken);

        // Novelties
        var novelties = await RetryAsync(() => apiSource.GetNoveltiesAsync(cancellationToken), "Novelties");
        await apiCache.SaveToCacheAsync("novelties.json", novelties, cancellationToken);

        // Titles
        var titles = await RetryAsync(() => apiSource.GetTitlesAsync(cancellationToken), "Titles");
        await apiCache.SaveToCacheAsync("titles.json", titles, cancellationToken);
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
        var data = await wikiSource.GetAllUnlocks([
            "Weaver's Sword (skin)"
            //"Mini Exalted Sage",
            //"Luminate's Backplate (skin)"
            //, "Endless Exalted Caster Tonic"
            ], cancellationToken);
        await wikiCache.SaveToCacheAsync("wiki.json", data, cancellationToken);
    }
}