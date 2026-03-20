namespace Gw2Unlocks.Api.Testing;

using GuildWars2.Collections;
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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


    public Task SaveToCacheAsync<T>(string fileName, IImmutableValueSet<T> data, CancellationToken cancellationToken)
    {
        // Save into the appropriate property depending on type
        switch (typeof(T).Name)
        {
            case nameof(Item):
                SavedItems = new ReadOnlyCollection<Item>([.. data.Cast<Item>()]);
                break;
            case nameof(Achievement):
                SavedAchievements = new ReadOnlyCollection<Achievement>([.. data.Cast<Achievement>()]);
                break;
            case nameof(Miniature):
                SavedMiniatures = new ReadOnlyCollection<Miniature>([.. data.Cast<Miniature>()]);
                break;
            case nameof(Novelty):
                SavedNovelties = new ReadOnlyCollection<Novelty>([.. data.Cast<Novelty>()]);
                break;
            case nameof(Title):
                SavedTitles = new ReadOnlyCollection<Title>([.. data.Cast<Title>()]);
                break;
        }

        return Task.CompletedTask;
    }
}