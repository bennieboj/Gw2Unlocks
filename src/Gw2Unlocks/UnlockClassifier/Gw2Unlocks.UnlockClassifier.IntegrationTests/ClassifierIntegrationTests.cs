using GuildWars2.Hero.Achievements.Rewards;
using GuildWars2.Hero.Builds;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.WikiProcessing.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
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
        Assert.IsAssignableFrom<Miniature>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldInGemStoreThenShouldReturnGemStore()
    {
        var unlockName = "Aurene's Crystalline Claws (heavy skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Gem Store");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldByVendorForTokenInZoneWhenClassifyingUnlockThenShouldReturnZone()
    {
        var unlockName = "Endless Spotted Beetle Tonic";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Dragon's Stand");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<Novelty>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldByVendorInZoneForCommonCurrencyWhenClassifyingUnlockThenShouldReturnZone()
    {
        var unlockName = "Mini Whisper of Jormag";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Icebrood Saga");
        var category = group.UnlockCategories.Single(c => c.Name == "Bjora Marches");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<Miniature>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenBloodRubyBackpackSoldInBloodstoneFenShouldLinkToCorrectCategory()
    {
        var unlockName = "Blood Ruby Backpack (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "LW Season 3");
        var category = group.UnlockCategories.Single(c => c.Name == "Bloodstone Fen");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Theory]
    [InlineData("Painter's Brilliance Axe")] // skin
    [InlineData("Abaddon Axe (skin)")]
    [InlineData("Collapsing Star Spear")] //skin
    [InlineData("Chiroptophobia")] //skin
    public async Task GivenSkinSoldForBlackLionClaimTicketShouldLinkToBlackLionClaimTicket(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Black Lion Claim Ticket");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<WeaponSkin>(unlock.ApiData);
    }

    [Theory]
    [InlineData("Golden Talon")] // skin
    [InlineData("Fuzzy Leopard Hat (heavy skin)")]
    public async Task GivenSkinSoldForBlackLionStatuettesShouldLinkToBlackLionStatuettes(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Black Lion Statuette");
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
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
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
        Assert.IsAssignableFrom<Novelty>(unlock.ApiData);
    }

    [Fact]
    public async Task BlueChoyaKiteIsCrystalOasis()
    {
        var unlockName = "Blue Choya Kite";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Path of Fire");
        var category = group.UnlockCategories.Single(c => c.Name == "Crystal Oasis");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<Novelty>(unlock.ApiData);
    }

    [Fact]
    public async Task MistShardVisageIsDragonFall()
    {
        var unlockName = "Mist Shard Visage";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "LW Season 4");
        var category = group.UnlockCategories.Single(c => c.Name == "Dragonfall");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Fact]
    public async Task FuneraryAxeSkinIsDesertHighlands()
    {
        var unlockName = "Funerary Axe (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Path of Fire");
        var category = group.UnlockCategories.Single(c => c.Name == "Desert Highlands");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Fact]
    public async Task FlameWeaponsAreBlackCitadelIGuess()
    {
        var unlockName = "Flame Blade (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Cities");
        var category = group.UnlockCategories.Single(c => c.Name == "Black Citadel");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<WeaponSkin>(unlock.ApiData);
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
        Assert.IsAssignableFrom<Miniature>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockHavingSoldForFestivalCurrencyShouldLinkToFestivalCategory()
    {
        var unlockName = "Plush Zhaia Backpack (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Festivals");
        var category = group.UnlockCategories.Single(c => c.Name == "Halloween");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);
        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<BackItemSkin>(unlock.ApiData);
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
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
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
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
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
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
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
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Fact]
    public async Task MiniWithTypePropertyCaptalizedAndMultipleCostsShouldStillFindApiDataAndTakeVendorWithHighestMathcingCosts()
    {
        string unlockName = "Mini Tyrannus Maneater";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Visions of Eternity");
        var category = group.UnlockCategories.Single(c => c.Name == "Starlit Weald");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<Miniature>(unlock.ApiData);
    }


    [Theory]
    // used to do this: Ardent Glorious Breastplate (skin) -> Ardent Glorious Breastplate -> Gift of Competitive Dedication -> Glob of Condensed Spirit Energy -> Spirit Shard -> Airship Cargo -> Verdant Brink
    [InlineData("Ardent Glorious Breastplate (skin)")] //don't follow spirit shard!
    // used to do this: Tome of the Rubicon (skin) -> Tome of the Rubicon -> Piles of Bloodstone Dust -> Grand Exalted Chest -> Auric Basin
    [InlineData("Tome of the Rubicon (skin)")] //don't follow piles of bloodstone shard!
    [InlineData("Abyssal Scepter (skin)")]
    public async Task GivenItemWithRecipeSourceMysticForgeShouldBeCategoryMysticForge(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Other");
        var category = group.UnlockCategories.Single(c => c.Name == "Mystic Forge");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementWithRewardShouldTakeGroupAndCategoryLinkedToAchievementCategory()
    {
        var unlockSkinName = "Temple Gate (skin)";
        var unlockAchiName = "Seitung Province (achievements)#achievement6331";
        var unlocks = new string[] { unlockSkinName, unlockAchiName };
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlocks);
        var group = results.UnlockGroups.Single(g => g.Name == "End of Dragons");
        var category = group.UnlockCategories.Single(c => c.Name == "Seitung Province");
        var unlockSkin = category.Unlocks.Single(c => c.Name == unlockSkinName);
        var unlockAchi = category.Unlocks.Single(c => c.Name == unlockAchiName);

        Assert.NotNull(unlockSkin);
        Assert.NotNull(unlockSkin.ApiData);
        Assert.IsAssignableFrom<EquipmentSkin>(unlockSkin.ApiData);

        Assert.NotNull(unlockAchi);
        Assert.NotNull(unlockAchi.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlockAchi.ApiData);
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
        Assert.IsAssignableFrom<EquipmentSkin>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementWithRewardThenItemsRequiredForAchievementShouldInfluenceGroupAndCategory()
    {
        var unlocksForAchi = new List<string> { "Auric Axe (skin)", "Auric Longbow (skin)" };
        var unlockAchiName = "Basic Collections#achievement2262"; // Auric Weapons achievement
        var unlockRewardName = "Auric Backplate (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, [.. unlocksForAchi, unlockAchiName]);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlockAchi = category.Unlocks.Single(c => c.Name == unlockAchiName);
        var unlockReward = category.Unlocks.Single(c => c.Name == unlockRewardName);

        Assert.NotNull(unlockAchi);
        Assert.NotNull(unlockAchi.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlockAchi.ApiData);

        Assert.NotNull(unlockReward);
        Assert.NotNull(unlockReward.ApiData);
    }

    [Fact]
    public async Task GivenAchievementWithoutRewardThenItemsRequiredForAchievementShouldInfluenceGroupAndCategory()
    {
        var unlocksForAchi = new List<string> { "Bladed Greaves (skin)" };
        var unlockName = "Basic Collections#achievement2407"; // Bladed Armor achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, [.. unlocksForAchi, unlockName]);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Verdant Brink");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlock.ApiData);
        Assert.Equal("https://render.guildwars2.com/file/109A0AE76FCA3EBC03039BA668B90142CAB0DDA2/866109.png", ((AchievementWithReward)unlock.ApiData).IconUrl?.ToString());
    }

    [Fact]
    public async Task GivenAchievementsPartOfAchievementCategoryWhichIsLinkedtoUnlockCategory()
    {
        var unlockName = "Auric Basin (achievements)#achievement2292"; // Highest Gear achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
        var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementsRewardsTitleApiDataShouldContainAchievementAndTitle()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3221"; // Playing Chicken  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "LW Season 3");
        var category = group.UnlockCategories.Single(c => c.Name == "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlock.ApiData);
        Assert.Equal(3221, ((AchievementWithReward)unlock.ApiData).Id);
        Assert.Equal("Chicken Chaser", ((AchievementWithReward)unlock.ApiData).RewardName);
        Assert.Equal("/img/icon_title.png", ((AchievementWithReward)unlock.ApiData).RewardIcon);

    }

    [Fact]
    public async Task GivenAchievementsRewardsMasteryPointThenApiDataShouldContainRewardIconInApiData()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3214"; // Quirky Quaggan Quest  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "LW Season 3");
        var category = group.UnlockCategories.Single(c => c.Name == "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlock.ApiData);
        Assert.Equal("Maguuma", ((AchievementWithReward)unlock.ApiData).Rewards!.OfType<MasteryPointReward>().Single().Region);
        Assert.Equal("/img/mastery_Maguuma.png", ((AchievementWithReward)unlock.ApiData).RewardIcon);
    }

    [Fact]
    public async Task GivenAchievementsRewardsItemThenApiDataShouldContainRewardIconInApiData()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3188"; // Stay Unfrosty  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "LW Season 3");
        var category = group.UnlockCategories.Single(c => c.Name == "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlock.ApiData);
        Assert.Equal("Magic-Warped Packet", ((AchievementWithReward)unlock.ApiData).RewardName);
        Assert.Equal("https://render.guildwars2.com/file/C399F9556A9478EF32A491345C4DA07605AD49D6/1465576.png", ((AchievementWithReward)unlock.ApiData).RewardIcon);
    }

    [Fact]
    public async Task GivenAchievementsThenApiDataShouldContainIconInApiData()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3188"; // Stay Unfrosty  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = results.UnlockGroups.Single(g => g.Name == "LW Season 3");
        var category = group.UnlockCategories.Single(c => c.Name == "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.IsAssignableFrom<AchievementWithReward>(unlock.ApiData);
        Assert.Equal("https://render.guildwars2.com/file/136E663C59275A077ADD394C935F26091B065A51/1601931.png", ((AchievementWithReward)unlock.ApiData).IconUrl?.ToString());
    }
}
