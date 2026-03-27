using GuildWars2;
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
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
    public static async Task<Collection<T>> ToCollectionAsync<T>(this IAsyncEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return new Collection<T>(list);
    }
}

#pragma warning disable CA1812 // This class is instantiated via DI and not directly, so it may appear unused
internal sealed class Gw2Api(Gw2Client client) : IGw2ApiSource
#pragma warning restore CA1812 // This class is instantiated via DI and not directly, so it may appear unused
{
    public async Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
    {
        return await client.Hero.Achievements.GetAchievementsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToCollectionAsync();
    }

    public async Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return await client.Items.GetItemsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToCollectionAsync();
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
