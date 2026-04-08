
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Testing;
public class Gw2ApiSuccessResponseFake : IGw2ApiSource, IGw2ApiCache
{
    public Collection<Item> Items { get; private set; } = [];
    public Collection<EquipmentSkin> Skins { get; private set; } = [];
    public Collection<Achievement> Achievements { get; private set; } = [];
    public Collection<Miniature> Miniatures { get; private set; } = [];
    public Collection<Novelty> Novelties { get; private set; } = [];
    public Collection<Title> Titles { get; private set; } = [];

    public void SetItems(Collection<Item> items) => Items = items;
    public void SetSkins(Collection<EquipmentSkin> skins) => Skins = skins;
    public void SetAchievements(Collection<Achievement> achievements) => Achievements = achievements;
    public void SetMiniatures(Collection<Miniature> miniatures) => Miniatures = miniatures;
    public void SetNovelties(Collection<Novelty> novelties) => Novelties = novelties;
    public void SetTitles(Collection<Title> titles) => Titles = titles;

    public virtual Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Items);
    public virtual Task<Collection<EquipmentSkin>> GetSkinsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Skins);

    public virtual Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Achievements);

    public virtual Task<Collection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
        => Task.FromResult(Miniatures);

    public virtual Task<Collection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
        => Task.FromResult(Novelties);

    public virtual Task<Collection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
        => Task.FromResult(Titles);


    public Collection<Item>? SavedItems { get; private set; }
    public Collection<EquipmentSkin>? SavedSkins { get; private set; }
    public Collection<Achievement>? SavedAchievements { get; private set; }
    public Collection<Miniature>? SavedMiniatures { get; private set; }
    public Collection<Novelty>? SavedNovelties { get; private set; }
    public Collection<Title>? SavedTitles { get; private set; }


    public Task SaveItemsToCacheAsync(Collection<Item> data, CancellationToken cancellationToken)
    {
        SavedItems = [.. data];
        return Task.CompletedTask;
    }
    public Task SaveSkinsToCacheAsync(Collection<EquipmentSkin> data, CancellationToken cancellationToken)
    {
        SavedSkins = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveAchievementsToCacheAsync(Collection<Achievement> data, CancellationToken cancellationToken)
    {
        SavedAchievements = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveMiniaturesToCacheAsync(Collection<Miniature> data, CancellationToken cancellationToken)
    {
        SavedMiniatures = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveNoveltiesToCacheAsync(Collection<Novelty> data, CancellationToken cancellationToken)
    {
        SavedNovelties = [.. data];
        return Task.CompletedTask;
    }

    public Task SaveTitlesToCacheAsync(Collection<Title> data, CancellationToken cancellationToken)
    {
        SavedTitles = [.. data];
        return Task.CompletedTask;
    }
}