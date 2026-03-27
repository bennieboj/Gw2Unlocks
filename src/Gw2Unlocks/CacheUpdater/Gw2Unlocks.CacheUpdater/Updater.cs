using Gw2Unlocks.Api;
using Gw2Unlocks.Wiki;
using System;
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

    private static async Task<Collection<T>> RetryAsync<T>(Func<Task<Collection<T>>> action, string name)
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
        var items = await apiCache.GetItemsAsync(cancellationToken);
        var achis = await apiCache.GetAchievementsAsync(cancellationToken);
        var novelties = await apiCache.GetNoveltiesAsync(cancellationToken);
        var titles = await apiCache.GetTitlesAsync(cancellationToken);

        var allNames = minis.Select(i => i.Name)
            .Concat(items.Select(m => m.Name))
            .Concat(achis.Select(a => a.Name))
            .Concat(novelties.Select(n => n.Name))
            .Concat(titles.Select(t => t.Name))
            .ToList();


        var graph = await wikiCache.GetAcquisitionGraph([], null, CancellationToken.None);
        var first50 = allNames.Take(50).ToList();
        try
        {
            await wikiSource.GetAcquisitionGraph([.. first50], graph, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await wikiCache.SaveAcquisitionGraphToCacheAsync(graph, CancellationToken.None);
            throw;
        }

        await wikiCache.SaveAcquisitionGraphToCacheAsync(graph, CancellationToken.None);
    }
}