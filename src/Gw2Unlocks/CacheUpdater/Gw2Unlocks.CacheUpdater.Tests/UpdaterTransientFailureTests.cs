using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using Gw2Unlocks.Api.Testing;
using Gw2Unlocks.Api.Testing.Builders;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.CacheUpdater.Tests;

public class UpdaterTransientFailureTests : ServiceProviderBasedTest<IUpdater>
{
    private readonly Gw2ApiTransientFailingResponseFake source;
    private readonly Gw2ApiSuccessResponseFake cache;

    public UpdaterTransientFailureTests(ITestOutputHelper output) : base(output)
    {
        source = (Gw2ApiTransientFailingResponseFake)GetService<IGw2ApiSource>();
        cache = (Gw2ApiSuccessResponseFake)GetService<IGw2ApiCache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddFakeApiSourceTransient()
                .AddFakeApiCacheSuccess()
                .AddFakeWikiSourceSuccess()
                .AddFakeWikiCacheSuccess()
                .AddUpdater();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task EndpointsFailUntilMaxOf5ThenSucceed(int failcount)
    {
        var sut = GetSut();
        source.FailCount = failcount;

        // Configure the “real” data
        source.ItemsData = new ReadOnlyCollection<Item>(
        [
            new ItemBuilder().WithName("Item 1").Build(),
            new ItemBuilder().WithName("Item 2").Build()
        ]);
        source.AchievementsData = new ReadOnlyCollection<Achievement>(
        [
            new AchievementBuilder().WithName("Ach 1").Build()
        ]);
        source.MiniaturesData = new ReadOnlyCollection<Miniature>(
        [
            new MiniatureBuilder().WithName("Mini 1").Build()
        ]);
        source.NoveltiesData = new ReadOnlyCollection<Novelty>(
        [
            new NoveltyBuilder().WithName("Novelty 1").Build()
        ]);
        source.TitlesData = new ReadOnlyCollection<Title>(
        [
            new TitleBuilder().WithName("Title 1").Build()
        ]);

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        // Validate writer has received the data after retry
        Assert.Equal(2, cache.SavedItems?.Count);
        Assert.Single(cache.SavedAchievements!);
        Assert.Single(cache.SavedMiniatures!);
        Assert.Single(cache.SavedNovelties!);
        Assert.Single(cache.SavedTitles!);
    }

    [Fact]
    public async Task EndpointsFailShouldThrowException()
    {
        var sut = GetSut();
        source.FailCount = 6;

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.UpdateApiData(TestContext.Current.CancellationToken));
    }
}