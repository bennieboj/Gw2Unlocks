using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Categories;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Gliders;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Outfits;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Hero.Emotes;
using GuildWars2.Items;
using Gw2Unlocks.Cache.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Cache;
internal sealed class Gw2ApiJsonCache(CachePaths cachePaths) : GenericCache(cachePaths.CacheDir, "api-cache"), IGw2ApiCache
{
    private const string itemsFileName = "items.json";
    private const string skinsFileName = "skins.json";
    private const string achievementsFileName = "achievements.json";
    private const string achievementCategoriesFileName = "achievementCategories.json";
    private const string miniaturesFileName = "miniatures.json";
    private const string noveltiesFileName = "novelties.json";
    private const string titlesFileName = "titles.json";
    private const string glidersFileName = "gliders.json";
    private const string outfitsFileName = "outfits.json";
    private const string emotesFileName = "emotes.json";



    private static Lazy<Task<Collection<Item>>>? _items;
    private static Lazy<Task<Collection<EquipmentSkin>>>? _skins;
    private static Lazy<Task<Collection<Achievement>>>? _achievements;
    private static Lazy<Task<Collection<AchievementCategory>>>? _achievementCategories;
    private static Lazy<Task<Collection<Miniature>>>? _miniatures;
    private static Lazy<Task<Collection<Novelty>>>? _novelties;
    private static Lazy<Task<Collection<Title>>>? _titles;
    private static Lazy<Task<Collection<GliderSkin>>>? _gliders;
    private static Lazy<Task<Collection<Outfit>>>? _outfits;
    private static Lazy<Task<Collection<Emote>>>? _emotes;

    public Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken)
        => (_items ??= new Lazy<Task<Collection<Item>>>(() =>
            LoadFromFileAsync<Collection<Item>>(itemsFileName, cancellationToken)))
        .Value;

    public Task<Collection<EquipmentSkin>> GetSkinsAsync(CancellationToken cancellationToken)
        => (_skins ??= new Lazy<Task<Collection<EquipmentSkin>>>(() =>
            LoadFromFileAsync<Collection<EquipmentSkin>>(skinsFileName, cancellationToken)))
        .Value;

    public Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
        => (_achievements ??= new Lazy<Task<Collection<Achievement>>>(() =>
            LoadFromFileAsync<Collection<Achievement>>(achievementsFileName, cancellationToken)))
        .Value;
    public Task<Collection<AchievementCategory>> GetAchievementCategoriesAsync(CancellationToken cancellationToken)
        => (_achievementCategories ??= new Lazy<Task<Collection<AchievementCategory>>>(() =>
            LoadFromFileAsync<Collection<AchievementCategory>>(achievementCategoriesFileName, cancellationToken)))
        .Value;

    public Task<Collection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
        => (_miniatures ??= new Lazy<Task<Collection<Miniature>>>(() =>
            LoadFromFileAsync<Collection<Miniature>>(miniaturesFileName, cancellationToken)))
        .Value;

    public Task<Collection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
        => (_novelties ??= new Lazy<Task<Collection<Novelty>>>(() =>
            LoadFromFileAsync<Collection<Novelty>>(noveltiesFileName, cancellationToken)))
        .Value;

    public Task<Collection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
        => (_titles ??= new Lazy<Task<Collection<Title>>>(() =>
            LoadFromFileAsync<Collection<Title>>(titlesFileName, cancellationToken)))
        .Value;

    public Task<Collection<GliderSkin>> GetGlidersAsync(CancellationToken cancellationToken)
        => (_gliders ??= new Lazy<Task<Collection<GliderSkin>>>(() =>
            LoadFromFileAsync<Collection<GliderSkin>>(glidersFileName, cancellationToken)))
        .Value;

    public Task<Collection<Outfit>> GetOutfitsAsync(CancellationToken cancellationToken)
        => (_outfits ??= new Lazy<Task<Collection<Outfit>>>(() =>
            LoadFromFileAsync<Collection<Outfit>>(outfitsFileName, cancellationToken)))
        .Value;

    public Task<Collection<Emote>> GetEmotesAsync(CancellationToken cancellationToken)
        => (_emotes ??= new Lazy<Task<Collection<Emote>>>(() =>
            LoadFromFileAsync<Collection<Emote>>(emotesFileName, cancellationToken)))
        .Value;


    public Task SaveItemsToCacheAsync(Collection<Item> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(itemsFileName, data, cancellationToken);
    public Task SaveSkinsToCacheAsync(Collection<EquipmentSkin> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(skinsFileName, data, cancellationToken);
    public Task SaveAchievementsToCacheAsync(Collection<Achievement> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(achievementsFileName, data, cancellationToken);
    public Task SaveAchievementCategoriesToCacheAsync(Collection<AchievementCategory> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(achievementCategoriesFileName, data, cancellationToken);
    public Task SaveMiniaturesToCacheAsync(Collection<Miniature> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(miniaturesFileName, data, cancellationToken);
    public Task SaveNoveltiesToCacheAsync(Collection<Novelty> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(noveltiesFileName, data, cancellationToken);
    public Task SaveTitlesToCacheAsync(Collection<Title> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(titlesFileName, data, cancellationToken);
    public Task SaveGlidersToCacheAsync(Collection<GliderSkin> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(glidersFileName, data, cancellationToken);
    public Task SaveOutfitsToCacheAsync(Collection<Outfit> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(outfitsFileName, data, cancellationToken);
    public Task SaveEmotesToCacheAsync(Collection<Emote> data, CancellationToken cancellationToken) => SaveToCacheJsonAsync(emotesFileName, data, cancellationToken);
}