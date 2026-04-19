using GuildWars2;
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Categories;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Implementation;

public static class AsyncEnumerableExtensions
{
    public static async Task<Collection<T>> ToCollectionAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = new List<T>();

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }

        return new Collection<T>(list);
    }
}

internal sealed class Gw2Api(Gw2Client client) : IGw2ApiSource
{
    public async Task<Collection<AchievementCategory>> GetAchievementCategoriesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Achievements.GetAchievementCategories(cancellationToken: cancellationToken).ValueOnly();
        return [.. set];
    }

    public async Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
    {
        return await client.Hero.Achievements.GetAchievementsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToCollectionAsync(cancellationToken);
    }

    public async Task<Collection<EquipmentSkin>> GetSkinsAsync(CancellationToken cancellationToken)
    {
        return await client.Hero.Equipment.Wardrobe.GetSkinsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToCollectionAsync(cancellationToken);
    }

    public async Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return await client.Items.GetItemsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToCollectionAsync(cancellationToken);
    }

    public async Task<Collection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Equipment.Miniatures.GetMiniatures(cancellationToken: cancellationToken).ValueOnly();
        return [.. set];
    }

    public async Task<Collection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Equipment.Novelties.GetNovelties(cancellationToken: cancellationToken).ValueOnly();
        return [.. set];
    }

    public async Task<Collection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Achievements.GetTitles(cancellationToken: cancellationToken).ValueOnly();
        return [.. set];
    }
}
