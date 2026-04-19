using GuildWars2.Items;
using Gw2Unlocks.Api;
using Gw2Unlocks.Wiki;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal class Updater(IGw2ApiSource apiSource, IGw2ApiCache apiCache, IGw2WikiSource wikiSource, IGw2WikiCache wikiCache, ILogger<Updater> logger) : IUpdater
{
    private const int MaxRetries = 5;

    public async Task UpdateApiData(CancellationToken cancellationToken)
    {
        // Items
        var items = await RetryAsync(() => apiSource.GetItemsAsync(cancellationToken), "Items");
        await apiCache.SaveItemsToCacheAsync(items, cancellationToken);

        // Skins
        var skins = await RetryAsync(() => apiSource.GetSkinsAsync(cancellationToken), "Skins");
        await apiCache.SaveSkinsToCacheAsync(skins, cancellationToken);

        // Achievements
        var achievements = await RetryAsync(() => apiSource.GetAchievementsAsync(cancellationToken), "Achievements");
        await apiCache.SaveAchievementsToCacheAsync(achievements, cancellationToken);

        // Achievement Categories
        var achievementCategories = await RetryAsync(() => apiSource.GetAchievementCategoriesAsync(cancellationToken), "Achievement Categories");
        await apiCache.SaveAchievementCategoriesToCacheAsync(achievementCategories, cancellationToken);

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
        await wikiCache.StreamPagesToCacheAsync(
            wikiSource.StreamAllPages(cancellationToken),
            cancellationToken
        );

        logger.LogInformation("Fetched all page names from wiki.");
    }
}