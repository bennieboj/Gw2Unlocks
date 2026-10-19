using Gw2Unlocks.Api;
using Gw2Unlocks.IconSpriteSheet;
using Gw2Unlocks.Wiki;
using GuildWars2.Items;
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
        var gliders = await RetryAsync(() => apiSource.GetGlidersAsync(cancellationToken), "Gliders");
        logger.LogInformation("Fetched {count} gliders from API.", gliders.Count);
        var outfits = await RetryAsync(() => apiSource.GetOutfitsAsync(cancellationToken), "Outfits");
        logger.LogInformation("Fetched {count} outfits from API.", outfits.Count);
        var emotes = await RetryAsync(() => apiSource.GetEmotesAsync(cancellationToken), "Emotes");
        logger.LogInformation("Fetched {count} emotes from API.", emotes.Count);

        List<IconSpriteSheetInput> input = [];
        input.AddRange(miniatures.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Miniature", x.Id, x.IconUrl!)));
        input.AddRange(skins.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Skin", x.Id, x.IconUrl!)));
        //input.AddRange(items.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Item", x.Id, x.IconUrl!)));
        //input.AddRange(achievements.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Achievement", x.Id, x.IconUrl!)));
        input.AddRange(achievementCategories.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("AchievementCategory", x.Id, x.IconUrl!)));
        input.AddRange(novelties.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Novelty", x.Id, x.IconUrl!)));
        // Recipe sheets are items; the sheet carries the icon and unlocks a recipe.
        input.AddRange(items.OfType<RecipeSheet>()
            .Where(x => x.IconUrl != null)
            .Select(x => new IconSpriteSheetInput("Recipe", x.RecipeId, x.IconUrl!)));
        input.AddRange(gliders.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Glider", x.Id, x.IconUrl!)));
        input.AddRange(outfits.Where(x => x.IconUrl != null).Select(x => new IconSpriteSheetInput("Outfit", x.Id, x.IconUrl!)));
        // Emotes carry no icon; use the icon of each item that unlocks the emote.
        var emoteUnlockItemIds = emotes.SelectMany(e => e.UnlockItemIds ?? []).ToHashSet();
        input.AddRange(items
            .Where(i => emoteUnlockItemIds.Contains(i.Id) && i.IconUrl != null)
            .Select(i => new IconSpriteSheetInput("Emote", i.Id, i.IconUrl!)));
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
        await apiCache.SaveGlidersToCacheAsync(gliders, cancellationToken);
        await apiCache.SaveOutfitsToCacheAsync(outfits, cancellationToken);
        await apiCache.SaveEmotesToCacheAsync(emotes, cancellationToken);
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