using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using Gw2Unlocks.Cache.Common;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Cache;
#pragma warning disable CA1812 // This class is instantiated via DI and not directly, so it may appear unused
internal sealed class Gw2ApiJsonCache : GenericCache, IGw2ApiCache
#pragma warning restore CA1812 // This class is instantiated via DI and not directly, so it may appear unused
{
    private const string itemsFileName = "items.json";
    private const string achievementsFileName = "achievements.json";
    private const string miniaturesFileName = "miniatures.json";
    private const string noveltiesFileName = "novelties.json";
    private const string titlesFileName = "titles.json";

    public Gw2ApiJsonCache() : base("api-cache")
    {
    }

    public Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<ReadOnlyCollection<Item>>(itemsFileName, cancellationToken);
    public Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<ReadOnlyCollection<Achievement>>(achievementsFileName, cancellationToken);
    public Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<ReadOnlyCollection<Miniature>>(miniaturesFileName, cancellationToken);
    public Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<ReadOnlyCollection<Novelty>>(noveltiesFileName, cancellationToken);
    public Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<ReadOnlyCollection<Title>>(titlesFileName, cancellationToken);


    public Task SaveItemsToCacheAsync(ReadOnlyCollection<Item> data, CancellationToken cancellationToken) => SaveToCacheAsync<ReadOnlyCollection<Item>>(itemsFileName, data, cancellationToken);
    public Task SaveAchievementsToCacheAsync(ReadOnlyCollection<Achievement> data, CancellationToken cancellationToken) => SaveToCacheAsync<ReadOnlyCollection<Achievement>>(achievementsFileName, data, cancellationToken);
    public Task SaveMiniaturesToCacheAsync(ReadOnlyCollection<Miniature> data, CancellationToken cancellationToken) => SaveToCacheAsync<ReadOnlyCollection<Miniature>>(miniaturesFileName, data, cancellationToken);
    public Task SaveNoveltiesToCacheAsync(ReadOnlyCollection<Novelty> data, CancellationToken cancellationToken) => SaveToCacheAsync<ReadOnlyCollection<Novelty>>(noveltiesFileName, data, cancellationToken);
    public Task SaveTitlesToCacheAsync(ReadOnlyCollection<Title> data, CancellationToken cancellationToken) => SaveToCacheAsync<ReadOnlyCollection<Title>>(titlesFileName, data, cancellationToken);
}