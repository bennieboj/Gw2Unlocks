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
    public static async Task<ReadOnlyCollection<T>> ToReadOnlyCollectionAsync<T>(this IAsyncEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return new ReadOnlyCollection<T>(list);
    }

    public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IImmutableSet<T> set)
    {
        ArgumentNullException.ThrowIfNull(set);
        return new ReadOnlyCollection<T>([.. set]);
    }
}


#pragma warning disable CA1812 // This class is instantiated via DI and not directly, so it may appear unused
internal sealed class Gw2Api(Gw2Client client) : IGw2ApiSource
#pragma warning restore CA1812 // This class is instantiated via DI and not directly, so it may appear unused
{
    public async Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
    {
        return await client.Hero.Achievements.GetAchievementsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToReadOnlyCollectionAsync();
    }

    public async Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return await client.Items.GetItemsBulk(cancellationToken: cancellationToken).ValueOnly(cancellationToken).ToReadOnlyCollectionAsync();
    }

    public async Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Equipment.Miniatures.GetMiniatures(cancellationToken: cancellationToken).ValueOnly();
        return new ReadOnlyCollection<Miniature>([.. set]);
    }

    public async Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Equipment.Novelties.GetNovelties(cancellationToken: cancellationToken).ValueOnly();
        return new ReadOnlyCollection<Novelty>([.. set]);
    }

    public async Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
    {
        var set = await client.Hero.Achievements.GetTitles(cancellationToken: cancellationToken).ValueOnly();
        return new ReadOnlyCollection<Title>([.. set]);
    }
}
