using Gw2Unlocks.Api;
using Gw2Unlocks.IconSpriteSheet;
using Gw2Unlocks.Wiki;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal class Updater(IGw2ApiSource apiSource, IGw2ApiCache apiCache,
                       IGw2WikiSource wikiSource, IGw2WikiCache wikiCache,
                       IIconSpriteSheetCache iconSpriteSheetCache,
                       IIconSpriteSheetGenerator iconSpriteSheetGenerator,
                       ILogger<Updater> logger) : IUpdater
{
    private const int MaxRetries = 5;

    public async Task UpdateApiData(CancellationToken cancellationToken)
    {
        var items = await RetryAsync(() => apiSource.GetItemsAsync(cancellationToken), "Items");
        logger.LogInformation("Fetched {count} items from API.", items.Count);
        var skins = await RetryAsync(() => apiSource.GetSkinsAsync(cancellationToken), "Skins");
        logger.LogInformation("Fetched {count} skins from API.", skins.Count);
        var achievements = await RetryAsync(() => apiSource.GetAchievementsAsync(cancellationToken), "Achievements");
        logger.LogInformation("Fetched {count} achievements from API.", achievements.Count);
        var achievementCategories = await RetryAsync(() => apiSource.GetAchievementCategoriesAsync(cancellationToken), "Achievement Categories");
        logger.LogInformation("Fetched {count} achievement categories from API.", achievementCategories.Count);
        var miniatures = await RetryAsync(() => apiSource.GetMiniaturesAsync(cancellationToken), "Miniatures");
        logger.LogInformation("Fetched {count} miniatures from API.", miniatures.Count);
        var novelties = await RetryAsync(() => apiSource.GetNoveltiesAsync(cancellationToken), "Novelties");
        logger.LogInformation("Fetched {count} novelties from API.", novelties.Count);
        var titles = await RetryAsync(() => apiSource.GetTitlesAsync(cancellationToken), "Titles");
        logger.LogInformation("Fetched {count} titles from API.", titles.Count);

        List<IconSpriteSheetInput> input = [];
        input.AddRange(miniatures.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Miniature", x.Id, x.IconUrl!)));
        input.AddRange(skins.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Skin", x.Id, x.IconUrl!)));
        //input.AddRange(items.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Item", x.Id, x.IconUrl!)));
        //input.AddRange(achievements.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Achievement", x.Id, x.IconUrl!)));
        input.AddRange(achievementCategories.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("AchievementCategory", x.Id, x.IconUrl!)));
        input.AddRange(novelties.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Novelty", x.Id, x.IconUrl!)));
        var sw = Stopwatch.StartNew();
        var iconSpriteSheetData = await iconSpriteSheetGenerator.Generate(input, cancellationToken);
        logger.LogInformation("iconSpriteSheetData generation time: {totalseconds}", sw.Elapsed.TotalSeconds);
        

        await iconSpriteSheetCache.SaveIconSpreadSheets(iconSpriteSheetData.Files, cancellationToken);
        await iconSpriteSheetCache.SaveIconSpriteSheetInventory(iconSpriteSheetData.InventoryData, cancellationToken);

        await apiCache.SaveItemsToCacheAsync(items, cancellationToken);
        await apiCache.SaveSkinsToCacheAsync(skins, cancellationToken);
        await apiCache.SaveAchievementsToCacheAsync(achievements, cancellationToken);
        await apiCache.SaveAchievementCategoriesToCacheAsync(achievementCategories, cancellationToken);
        await apiCache.SaveMiniaturesToCacheAsync(miniatures, cancellationToken);
        await apiCache.SaveNoveltiesToCacheAsync(novelties, cancellationToken);
        await apiCache.SaveTitlesToCacheAsync(titles, cancellationToken);
    }

    private async Task<Collection<T>> RetryAsync<T>(Func<Task<Collection<T>>> action, string name)
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
                logger.LogInformation("Attempt {attempt} for {name} failed: {exMessage}. Retrying...", attempt, name, ex.Message);
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