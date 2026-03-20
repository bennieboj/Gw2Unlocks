using GuildWars2.Collections;
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal class Updater(IGw2ApiSource reader, IGw2ApiCache writer) : IUpdater
{
    private const int MaxRetries = 5;

    public async Task UpdateApiData(CancellationToken cancellationToken)
    {
        // Items
        var items = await RetryAsync(() => reader.GetItemsAsync(cancellationToken), "Items");
        await writer.SaveToCacheAsync("items.json", new ImmutableValueSet<Item>(items), cancellationToken);

        // Achievements
        var achievements = await RetryAsync(() => reader.GetAchievementsAsync(cancellationToken), "Achievements");
        await writer.SaveToCacheAsync("achievements.json", new ImmutableValueSet<Achievement>(achievements), cancellationToken);

        // Miniatures
        var miniatures = await RetryAsync(() => reader.GetMiniaturesAsync(cancellationToken), "Miniatures");
        await writer.SaveToCacheAsync("miniatures.json", new ImmutableValueSet<Miniature>(miniatures), cancellationToken);

        // Novelties
        var novelties = await RetryAsync(() => reader.GetNoveltiesAsync(cancellationToken), "Novelties");
        await writer.SaveToCacheAsync("novelties.json", new ImmutableValueSet<Novelty>(novelties), cancellationToken);

        // Titles
        var titles = await RetryAsync(() => reader.GetTitlesAsync(cancellationToken), "Titles");
        await writer.SaveToCacheAsync("titles.json", new ImmutableValueSet<Title>(titles), cancellationToken);
    }

    private static async Task<ReadOnlyCollection<T>> RetryAsync<T>(Func<Task<ReadOnlyCollection<T>>> action, string name)
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
}