
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Testing;
public class Gw2ApiSuccessResponseFake : IGw2ApiSource, IGw2ApiCache
{
    public ReadOnlyCollection<Item> Items { get; set; } = new ReadOnlyCollection<Item>(Array.Empty<Item>());
    public ReadOnlyCollection<Achievement> Achievements { get; set; } = new ReadOnlyCollection<Achievement>(Array.Empty<Achievement>());
    public ReadOnlyCollection<Miniature> Miniatures { get; set; } = new ReadOnlyCollection<Miniature>(Array.Empty<Miniature>());
    public ReadOnlyCollection<Novelty> Novelties { get; set; } = new ReadOnlyCollection<Novelty>(Array.Empty<Novelty>());
    public ReadOnlyCollection<Title> Titles { get; set; } = new ReadOnlyCollection<Title>(Array.Empty<Title>());

    public virtual Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Items);

    public virtual Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Achievements);

    public virtual Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
        => Task.FromResult(Miniatures);

    public virtual Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
        => Task.FromResult(Novelties);

    public virtual Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
        => Task.FromResult(Titles);


    public ReadOnlyCollection<Item>? SavedItems { get; private set; }
    public ReadOnlyCollection<Achievement>? SavedAchievements { get; private set; }
    public ReadOnlyCollection<Miniature>? SavedMiniatures { get; private set; }
    public ReadOnlyCollection<Novelty>? SavedNovelties { get; private set; }
    public ReadOnlyCollection<Title>? SavedTitles { get; private set; }


    public Task SaveItemsToCacheAsync(ReadOnlyCollection<Item> data, CancellationToken cancellationToken)
    {
        SavedItems = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveAchievementsToCacheAsync(ReadOnlyCollection<Achievement> data, CancellationToken cancellationToken)
    {
        SavedAchievements = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveMiniaturesToCacheAsync(ReadOnlyCollection<Miniature> data, CancellationToken cancellationToken)
    {
        SavedMiniatures = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveNoveltiesToCacheAsync(ReadOnlyCollection<Novelty> data, CancellationToken cancellationToken)
    {
        SavedNovelties = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveTitlesToCacheAsync(ReadOnlyCollection<Title> data, CancellationToken cancellationToken)
    {
        SavedTitles = [.. data];
        return Task.CompletedTask;
    }
}