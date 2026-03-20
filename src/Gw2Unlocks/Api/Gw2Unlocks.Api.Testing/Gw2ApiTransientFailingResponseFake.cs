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

public class Gw2ApiTransientFailingResponseFake : IGw2ApiSource
{
    // Configurable data
    public ReadOnlyCollection<Item>? ItemsData { get; set; }
    public ReadOnlyCollection<Achievement>? AchievementsData { get; set; }
    public ReadOnlyCollection<Miniature>? MiniaturesData { get; set; }
    public ReadOnlyCollection<Novelty>? NoveltiesData { get; set; }
    public ReadOnlyCollection<Title>? TitlesData { get; set; }

    // Shared fail configuration
    public int FailCount { get; set; } = 1;

    // Internal counter
    private int attempts;

    private void ThrowIfNeeded(string message)
    {
        attempts++;
        if (attempts <= FailCount)
            throw new InvalidOperationException(message);
    }

    public Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Items failure");
        return Task.FromResult(ItemsData ?? new ReadOnlyCollection<Item>(Array.Empty<Item>()));
    }

    public Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Achievements failure");
        return Task.FromResult(AchievementsData ?? new ReadOnlyCollection<Achievement>(Array.Empty<Achievement>()));
    }

    public Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Miniatures failure");
        return Task.FromResult(MiniaturesData ?? new ReadOnlyCollection<Miniature>(Array.Empty<Miniature>()));
    }

    public Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Novelties failure");
        return Task.FromResult(NoveltiesData ?? new ReadOnlyCollection<Novelty>(Array.Empty<Novelty>()));
    }

    public Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Titles failure");
        return Task.FromResult(TitlesData ?? new ReadOnlyCollection<Title>(Array.Empty<Title>()));
    }
}