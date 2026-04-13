using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.WikiProcessing.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.UnlockClassifier.IntegrationTests;

public class ClassifierIntegrationTests(ITestOutputHelper output) : ServiceProviderBasedTest<IClassifier>(output, LogLevel.Information)
{
    protected override void Configure(IServiceCollection services)
    {
        services.AddCacheDir()
                .AddJsonCacheApiSource()
                .AddJsonCacheWikiProcessingSource()
                .AddClassifier();
    }

    /// Mini Exalted Sage, sold by Exalted Mastery Vendor
    /// Exalted Mastery Vendor is present in both Verdant Brink and Auric Basin
    /// Bus since the Mini Exalted Sage is sold for Lump of Aurilium, which is only acquired in Auric Basin
    /// the unlock should be classified as Auric Basin.
    [Fact]
    public async Task GivenUnlockSoldBySameVendorInMultipleZonesWhenClassifyingUnlockThenShouldReturnZoneLinkedToSellingCurrency()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Mini Exalted Sage");
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == "Mini Exalted Sage");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task NoveltiesWork()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Endless Exalted Caster Tonic");
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == "Endless Exalted Caster Tonic");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockHavingRecipeWithTokenAsIngredientShouldLinkToCategory()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Mini Foostivoo the Merry");
        var group = results.UnlockGroups.Single(g => g.Name == "Festivals");
        var category = group.UnlockCategories.Single(c => c.Name == "Wintersday");
        var unlock = category.Unlocks.Single(c => c.Name == "Mini Foostivoo the Merry");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockContainsExoticChestShouldBeGeneralCategory()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Adam (skin)");
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "General");
        var unlock = category.Unlocks.Single(c => c.Name == "Adam (skin)");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSkinShouldContainApiData()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Bladed Helmet (skin)");
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Verdant Brink");
        var unlock = category.Unlocks.Single(c => c.Name == "Bladed Helmet (skin)");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }


    [Theory]
    [InlineData("Sunspear Warsickle (skin)")]
    [InlineData("Leather Coat (skin)")]
    public async Task CraftingShouldBeCrafting(string unlockname)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockname);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Crafting");
        var unlock = category.Unlocks.Single(c => c.Name == unlockname);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenItemWithRecipeSourceMysticForrgeShouldBeCategoryMysticForge()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Abyssal Scepter (skin)");
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Mystic Forge");
        var unlock = category.Unlocks.Single(c => c.Name == "Abyssal Scepter (skin)");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenItemAwardedByAchievementShouldTakeGroupAndCategoryOfAchievement()
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Temple Gate (skin)");
        var group = results.UnlockGroups.Single(g => g.Name == "Path of Fire");
        var category = group.UnlockCategories.Single(c => c.Name == "Seitung Province");
        var unlock = category.Unlocks.Single(c => c.Name == "Temple Gate (skin)");

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }
}
