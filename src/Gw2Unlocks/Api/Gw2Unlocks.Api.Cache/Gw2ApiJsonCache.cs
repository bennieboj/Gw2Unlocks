namespace Gw2Unlocks.Api.Cache;

using GuildWars2;
using GuildWars2.Collections;
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using Gw2Unlocks.Cache;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1812 // This class is instantiated via DI and not directly, so it may appear unused
internal sealed class Gw2ApiJsonCache : IGw2ApiSource, IGw2ApiCache
#pragma warning restore CA1812 // This class is instantiated via DI and not directly, so it may appear unused
{
    private readonly string _cacheDir;

    public Gw2ApiJsonCache()
    {
        _cacheDir = Path.Combine(CachePaths.Root, "api-cache");
        Directory.CreateDirectory(_cacheDir);
    }

    // --- Generic helper to read JSON ---
    private async Task<ReadOnlyCollection<T>> LoadFromFileAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_cacheDir, fileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Cache file not found: {path}");

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var data = JsonSerializer.Deserialize<List<T>>(json)!;
        return new ReadOnlyCollection<T>(data);
    }

    // --- Generic helper to save JSON from caller-provided data ---
    public async Task SaveToCacheAsync<T>(string fileName, IImmutableValueSet<T> data, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_cacheDir, fileName);
        var json = JsonSerializer.Serialize(data.ToList());
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    // --- IGw2Api implementation ---
    public Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Item>("items.json", cancellationToken);
    public Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Achievement>("achievements.json", cancellationToken);
    public Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Miniature>("miniatures.json", cancellationToken);
    public Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Novelty>("novelties.json", cancellationToken);
    public Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Title>("titles.json", cancellationToken);
}