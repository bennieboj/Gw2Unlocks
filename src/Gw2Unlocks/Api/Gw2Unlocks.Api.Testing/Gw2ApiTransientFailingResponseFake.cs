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

public class Gw2ApiTransientFailingResponseFake : IGw2ApiSource
{
    // Configurable data
    private Collection<Item> Items = [];
    private Collection<EquipmentSkin> Skins = [];
    private Collection<Achievement> Achievements = [];
    private Collection<Miniature> Miniatures = [];
    private Collection<Novelty> Novelties = [];
    private Collection<Title> Titles = [];

    public void SetItems(Collection<Item> items) => Items = items;
    public void SetSkins(Collection<EquipmentSkin> skins) => Skins = skins;
    public void SetAchievements(Collection<Achievement> achievements) => Achievements = achievements;
    public void SetMiniatures(Collection<Miniature> miniatures) => Miniatures = miniatures;
    public void SetNovelties(Collection<Novelty> novelties) => Novelties = novelties;
    public void SetTitles(Collection<Title> titles) => Titles = titles;

    // Shared fail configuration
    private int FailCount = 1;

    // Internal counter
    private int attempts;

    private void ThrowIfNeeded(string message)
    {
        attempts++;
        if (attempts <= FailCount)
            throw new InvalidOperationException(message);
    }

    public void SetFailCount(int count) => FailCount = count;

    public Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Items failure");
        return Task.FromResult(Items);
    }

    public Task<Collection<EquipmentSkin>> GetSkinsAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Skins failure");
        return Task.FromResult(Skins);
    }

    public Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Achievements failure");
        return Task.FromResult(Achievements);
    }

    public Task<Collection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Miniatures failure");
        return Task.FromResult(Miniatures);
    }

    public Task<Collection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Novelties failure");
        return Task.FromResult(Novelties);
    }

    public Task<Collection<Title>> GetTitlesAsync(CancellationToken cancellationToken)
    {
        ThrowIfNeeded("Transient Titles failure");
        return Task.FromResult(Titles);
    }
}