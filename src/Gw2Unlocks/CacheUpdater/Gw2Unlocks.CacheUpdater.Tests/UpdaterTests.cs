using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Categories;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
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
}

