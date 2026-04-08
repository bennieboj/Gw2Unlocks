using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using Gw2Unlocks.Cache.Common;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Cache;
internal sealed class Gw2ApiJsonCache(CachePaths cachePaths) : GenericCache(cachePaths, "api-cache"), IGw2ApiCache
{
    private const string itemsFileName = "items.json";
    private const string skinsFileName = "skins.json";
    private const string achievementsFileName = "achievements.json";
    private const string miniaturesFileName = "miniatures.json";
    private const string noveltiesFileName = "novelties.json";
    private const string titlesFileName = "titles.json";

    public Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Collection<Item>>(itemsFileName, cancellationToken);
    public Task<Collection<EquipmentSkin>> GetSkinsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Collection<EquipmentSkin>>(skinsFileName, cancellationToken);
    public Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Collection<Achievement>>(achievementsFileName, cancellationToken);
    public Task<Collection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Collection<Miniature>>(miniaturesFileName, cancellationToken);
    public Task<Collection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Collection<Novelty>>(noveltiesFileName, cancellationToken);
    public Task<Collection<Title>> GetTitlesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Collection<Title>>(titlesFileName, cancellationToken);


    public Task SaveItemsToCacheAsync(Collection<Item> data, CancellationToken cancellationToken) => SaveToCacheAsync(itemsFileName, data, cancellationToken);
    public Task SaveSkinsToCacheAsync(Collection<EquipmentSkin> data, CancellationToken cancellationToken) => SaveToCacheAsync(skinsFileName, data, cancellationToken);
    public Task SaveAchievementsToCacheAsync(Collection<Achievement> data, CancellationToken cancellationToken) => SaveToCacheAsync(achievementsFileName, data, cancellationToken);
    public Task SaveMiniaturesToCacheAsync(Collection<Miniature> data, CancellationToken cancellationToken) => SaveToCacheAsync(miniaturesFileName, data, cancellationToken);
    public Task SaveNoveltiesToCacheAsync(Collection<Novelty> data, CancellationToken cancellationToken) => SaveToCacheAsync(noveltiesFileName, data, cancellationToken);
    public Task SaveTitlesToCacheAsync(Collection<Title> data, CancellationToken cancellationToken) => SaveToCacheAsync(titlesFileName, data, cancellationToken);
}