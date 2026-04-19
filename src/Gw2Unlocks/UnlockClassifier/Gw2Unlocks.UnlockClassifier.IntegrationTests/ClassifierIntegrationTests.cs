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
        var unlockName = "Mini Exalted Sage";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Great Capra (skin)", "Verdant Brink")]
    [InlineData("Ley Guard's Protector", "Auric Basin")]
    [InlineData("Ley Guard's Revolver", "Auric Basin")]
    [InlineData("Augury of Death (skin)", "Auric Basin")]
    [InlineData("Plated Axe (skin)", "Dragon's Stand")]
    public async Task GivenUnlockInChestInZoneShouldResultInZone(string unlockName, string zoneName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == zoneName);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task NoveltiesWork()
    {
        var unlockName = "Endless Exalted Caster Tonic";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockHavingRecipeWithTokenAsIngredientShouldLinkToCategory()
    {
        var unlockName = "Mini Foostivoo the Merry";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Festivals");
        var category = group.UnlockCategories.Single(c => c.Name == "Wintersday");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);
        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockContainsExoticChestShouldBeGeneralCategory()
    {
        var unlockName = "Adam (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "General");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSkinShouldContainApiData()
    {
        var unlockName = "Bladed Helmet (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Bladed Helmet (skin)");
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Verdant Brink");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }


    [Fact]
    public async Task CraftingShouldBeCrafting()
    {
        string unlockName = "Leather Coat (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Crafting");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task ItemInMultipleClassificationsShouldUseMostCommon()
    {
        string unlockName = "Sunspear Warsickle (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Path of Fire");
        var unlock = group.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenItemWithRecipeSourceMysticForrgeShouldBeCategoryMysticForge()
    {
        var unlockName = "Abyssal Scepter (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Mystic Forge");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenItemAwardedByAchievementShouldTakeGroupAndCategoryLinkedToAchievementCategory()
    {
        var unlockName = "Temple Gate (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "End of Dragons");
        var category = group.UnlockCategories.Single(c => c.Name == "Seitung Province");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Auric Axe (skin)")]
    [InlineData("Auric Longbow (skin)")]
    public async Task ItemsRequiredForAchievementShouldCategorizeCorrectly(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenItemsAwardedByAchievementShouldInfluenceGroupAndCategory()
    {
        var unlockName = "Auric Backplate (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task ItemsRequiredForAchievementShouldInfluenceGroupAndCategory()
    {
        var unlockName = "Auric Weapons";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementsPartOfAchievementCategoryWhichIsLinkedtoUnlockCategory()
    {
        var unlockName = "Highest Gear";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }
}
