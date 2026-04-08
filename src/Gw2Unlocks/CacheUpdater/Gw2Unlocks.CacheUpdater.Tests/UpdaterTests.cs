using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using Gw2Unlocks.Api.Testing;
using Gw2Unlocks.Api.Testing.Builders;
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

    public UpdaterTests(ITestOutputHelper output) : base(output)
    {
        source = (Gw2ApiSuccessResponseFake)GetService<IGw2ApiSource>();
        cache = (Gw2ApiSuccessResponseFake)GetService<IGw2ApiCache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddFakeApiSourceSuccess()
                .AddFakeApiCacheSuccess()
                .AddFakeWikiSourceSuccess()
                .AddFakeWikiCacheSuccess()
                .AddUpdater();
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
}

