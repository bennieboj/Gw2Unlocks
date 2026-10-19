using GuildWars2;
using GuildWars2.Collections;
using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Categories;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Emotes;
using GuildWars2.Hero.Equipment.Gliders;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Outfits;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using Gw2Unlocks.Api.Testing;
using Gw2Unlocks.Api.Testing.Builders;
using Gw2Unlocks.IconSpriteSheet;
using Gw2Unlocks.IconSpriteSheet.Implementation;
using Gw2Unlocks.IconSpriteSheet.Testing;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.CacheUpdater.Tests;

public class UpdaterTests : ServiceProviderBasedTest<IUpdater>
{
    private readonly Gw2ApiSuccessResponseFake source;
    private readonly Gw2ApiSuccessResponseFake cache;
    private readonly FakeIconSpriteSheetCache iconSpreadSheetCache;

    public UpdaterTests(ITestOutputHelper output) : base(output)
    {
        source = (Gw2ApiSuccessResponseFake)GetService<IGw2ApiSource>();
        cache = (Gw2ApiSuccessResponseFake)GetService<IGw2ApiCache>();
        iconSpreadSheetCache = (FakeIconSpriteSheetCache)GetService<IIconSpriteSheetCache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddFakeApiSourceSuccess()
                .AddFakeApiCacheSuccess()
                .AddFakeWikiSourceSuccess()
                .AddFakeWikiCacheSuccess()
                .AddUpdater();

        services.AddIconSpriteSheetGenerator()
                .AddFakeIconSpriteSheetCache()
                .AddFakeIconSpriteSheetHttpMessagehandler()
                .AddIconSpriteSheetGeneratorHttpClient()
                .AddFakeHttpMessageHandler();
    }

    [Fact]
    public async Task CanFetchAndCacheAllEndpoints()
    {
        var sut = GetSut();

        source.SetItems(new Collection<Item>(
        [
            new ItemBuilder().WithName("Item 1").Build(),
            new ItemBuilder().WithName("Item 2").Build()
        ]));

        source.SetSkins(new Collection<EquipmentSkin>(
        [
            new SkinBuilder().WithName("Skin 1").Build()
        ]));

        source.SetAchievements(new Collection<Achievement>(
        [
            new AchievementBuilder().WithName("Ach 1").Build()
        ]));

        source.SetAchievementCategories(new Collection<AchievementCategory>(
        [
            new AchievementCategoryBuilder().WithName("Category 1").Build()
        ]));

        source.SetMiniatures(new Collection<Miniature>(
        [
            new MiniatureBuilder().WithName("Mini 1").Build()
        ]));

        source.SetNovelties(new Collection<Novelty>(
        [
            new NoveltyBuilder().WithName("Novelty 1").Build()
        ]));

        source.SetTitles(new Collection<Title>(
        [
            new TitleBuilder().WithName("Title 1").Build()
        ]));

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        Assert.Equal(2, cache.SavedItems?.Count);
        Assert.Single(cache.SavedSkins!);
        Assert.Single(cache.SavedAchievements!);
        Assert.Single(cache.SavedMiniatures!);
        Assert.Single(cache.SavedNovelties!);
        Assert.Single(cache.SavedTitles!);
    }
    [Fact]
    public async Task CanMakeIconSpreadSheet()
    {
        var sut = GetSut();

        source.SetItems(new Collection<Item>(
        [
            new ItemBuilder().WithName("Item 1").Build()
        ]));

        source.SetSkins(new Collection<EquipmentSkin>(
        [
            new SkinBuilder().WithName("Skin 1").WithId(1).Build(),
            new SkinBuilder().WithName("Skin 2").WithId(2).Build()
        ]));

        //source.SetItems(new Collection<Item>(
        //[
        //    new ItemBuilder().WithName("Item 1").WithId(1).Build(),
        //    new ItemBuilder().WithName("Item 2").WithId(2).Build()
        //]));

        //source.SetAchievements(new Collection<Achievement>(
        //[
        //    new AchievementBuilder().WithName("Ach 1").WithId(1).Build(),
        //    new AchievementBuilder().WithName("Ach 2").WithId(2).Build()
        //]));

        source.SetAchievementCategories(new Collection<AchievementCategory>(
        [
            new AchievementCategoryBuilder().WithName("Category 1").WithId(1).Build(),
            new AchievementCategoryBuilder().WithName("Category 2").WithId(2).Build()
        ]));

        source.SetMiniatures(new Collection<Miniature>(
        [
            new MiniatureBuilder().WithName("Mini 1").WithId(1).Build(),
            new MiniatureBuilder().WithName("Mini 2").WithId(2).Build()
        ]));

        source.SetNovelties(new Collection<Novelty>(
        [
            new NoveltyBuilder().WithName("Novelty 1").WithId(1).Build(),
            new NoveltyBuilder().WithName("Novelty 2").WithId(2).Build()
        ]));

        source.SetTitles(new Collection<Title>(
        [
            new TitleBuilder().WithName("Title 1").Build()
        ]));

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        var inv = iconSpreadSheetCache.SavedInventory.Inventory;
        Assert.True(inv.ContainsKey("Skin/1"));
        Assert.True(inv.ContainsKey("Skin/2"));
        //Assert.True(inv.ContainsKey("Item/1"));
        //Assert.True(inv.ContainsKey("Item/2"));
        //Assert.True(inv.ContainsKey("Achievement/1"));
        //Assert.True(inv.ContainsKey("Achievement/2"));
        Assert.True(inv.ContainsKey("AchievementCategory/1"));
        Assert.True(inv.ContainsKey("AchievementCategory/2"));
        Assert.True(inv.ContainsKey("Miniature/1"));
        Assert.True(inv.ContainsKey("Miniature/2"));
        Assert.True(inv.ContainsKey("Novelty/1"));
        Assert.True(inv.ContainsKey("Novelty/2"));
    }

    [Fact]
    public async Task DuplicateRecipeIdsFromDifferentSheetsCollapseToOneInventoryEntry()
    {
        var sut = GetSut();

        // Two different recipe-sheet items unlocking the same recipe (the Recipe/6472 crash).
        source.SetItems(new Collection<Item>(
        [
            new RecipeSheet
            {
                Id = 1001,
                Name = "Sheet A",
                Description = "Desc",
                Level = 0,
                Rarity = Rarity.Masterwork,
                VendorValue = Coin.Zero,
                GameTypes = ImmutableValueList<Extensible<GameType>>.Empty,
                Flags = new ItemFlagsBuilder().Build(),
                Restrictions = new ItemRestrictionBuilder().Build(),
                ChatLink = "dummy",
                IconUrl = new Uri("http://www.example.com/a.png"),
                RecipeId = 6472,
                ExtraRecipeIds = ImmutableValueList<int>.Empty,
            },
            new RecipeSheet
            {
                Id = 1002,
                Name = "Sheet B",
                Description = "Desc",
                Level = 0,
                Rarity = Rarity.Masterwork,
                VendorValue = Coin.Zero,
                GameTypes = ImmutableValueList<Extensible<GameType>>.Empty,
                Flags = new ItemFlagsBuilder().Build(),
                Restrictions = new ItemRestrictionBuilder().Build(),
                ChatLink = "dummy",
                IconUrl = new Uri("http://www.example.com/b.png"),
                RecipeId = 6472,
                ExtraRecipeIds = ImmutableValueList<int>.Empty,
            },
        ]));

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        var inv = iconSpreadSheetCache.SavedInventory.Inventory;
        Assert.True(inv.ContainsKey("Recipe/6472"));
    }

    [Fact]
    public async Task RecipeGliderOutfitAndEmoteUnlocksProduceSpriteInventoryEntries()
    {
        var sut = GetSut();

        source.SetItems(new Collection<Item>(
        [
            new RecipeSheet
            {
                Id = 2001,
                Name = "Recipe Sheet",
                Description = "Desc",
                Level = 0,
                Rarity = Rarity.Masterwork,
                VendorValue = Coin.Zero,
                GameTypes = ImmutableValueList<Extensible<GameType>>.Empty,
                Flags = new ItemFlagsBuilder().Build(),
                Restrictions = new ItemRestrictionBuilder().Build(),
                ChatLink = "dummy",
                IconUrl = new Uri("http://www.example.com/recipe.png"),
                RecipeId = 5001,
                ExtraRecipeIds = ImmutableValueList<int>.Empty,
            },
            new ItemBuilder().WithId(2002).WithName("Emote Tome").Build(),
        ]));

        source.SetGliders(new Collection<GliderSkin>(
        [
            new GliderSkin
            {
                Id = 64,
                Name = "Test Glider",
                Description = "Desc",
                IconUrl = new Uri("http://www.example.com/glider.png"),
                Order = 1,
                DefaultDyeColorIds = ImmutableValueList<int>.Empty,
                UnlockItemIds = ImmutableValueList.Create([9001]),
            },
        ]));

        source.SetOutfits(new Collection<Outfit>(
        [
            new Outfit
            {
                Id = 129,
                Name = "Test Outfit",
                IconUrl = new Uri("http://www.example.com/outfit.png"),
                UnlockItemIds = ImmutableValueList.Create([9002]),
            },
        ]));

        source.SetEmotes(new Collection<Emote>(
        [
            new Emote
            {
                Id = "testemote",
                Commands = ImmutableValueList.Create(["/testemote"]),
                UnlockItemIds = ImmutableValueList.Create([2002]),
            },
        ]));

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        var inv = iconSpreadSheetCache.SavedInventory.Inventory;
        Assert.True(inv.ContainsKey("Recipe/5001"));
        Assert.True(inv.ContainsKey("Glider/64"));
        Assert.True(inv.ContainsKey("Outfit/129"));
        Assert.True(inv.ContainsKey("Emote/2002"));
    }
}

