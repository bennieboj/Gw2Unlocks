using GuildWars2.Hero.Builds;
using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.IconSpriteSheet.Cache;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.UnlockClassifier.Implementation;
using Gw2Unlocks.WikiProcessing.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
                .AddIconSpriteSheetCache()
                .AddClassifier();
    }

    /// <summary>
    /// Finds the node at the given path, e.g. Find(config, "Heart of Thorns", "Auric Basin").
    /// The path is resolved level by level, so it also works for deeper nesting.
    /// </summary>
    private static UnlockCategory Find(ClassifyConfig config, params string[] path)
    {
        var siblings = config.Categories;
        UnlockCategory? node = null;

        foreach (var name in path)
        {
            node = Assert.Single(siblings, c => c.Name == name);
            siblings = node.SubCategories;
        }

        return node ?? throw new ArgumentException("Empty path", nameof(path));
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
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal(346, unlock.ApiData.Id);
        Assert.Equal(Type.Miniature, unlock.ApiData.Type);
        Assert.Equal("Mini Exalted Sage", unlock.ApiData.Name);
        Assert.NotNull(unlock.ApiData.IconUrl);
        Assert.NotNull(unlock.ApiData.IconSheet);
        Assert.NotNull(unlock.ApiData.IconX);
        Assert.NotNull(unlock.ApiData.IconY);
        Assert.Equal(74444, unlock.ApiData.ChatCodeId);
    }

    /// The Alliance Field Quartermaster sells different items at different locations
    /// (Shipwreck Strand, Starlit Weald, Eternity's Garden and Leyspring Hollows),
    /// each for a location-specific token. Even though the vendor itself is located
    /// in all of these zones, each unlock must be classified to the location where
    /// it is actually sold — and to none of the vendor's other zones.
    [Theory]
    [InlineData("Restless Captain's Heavy Veil (skin)", "Visions of Eternity", "Shipwreck Strand")]
    [InlineData("Extremis Heavy Hood (skin)", "Visions of Eternity", "Starlit Weald")]
    [InlineData("Forge Guard's Heavy Helmet (skin)", "Visions of Eternity", "Eternity's Garden")]
    [InlineData("Tenebral Ward Heavy Helmet (skin)", "Visions of Eternity", "Leyspring Hollows", Skip = "Wiki skin node not available")]
    public async Task GivenVendorSellsItemsAtDifferentLocationsWhenClassifyingUnlockThenShouldReturnCategoryLinkedToSaleLocation(string unlockName, string expansionName, string categoryName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var expansion = Find(results, "Expansions & Living World", expansionName);
        var category = Find(results, "Expansions & Living World", expansionName, categoryName);

        // The unlock must be classified into the category where it is actually sold.
        Assert.Contains(category.Unlocks, u => u.Name == unlockName);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);
        Assert.NotNull(unlock.ApiData);

        // The vendor is LocatedIn every zone it sells at: the unlock must not leak into those.
        Assert.DoesNotContain(expansion.SubCategories
            .Where(c => c.Name != categoryName)
            .SelectMany(c => c.Unlocks), u => u.Name == unlockName);
    }

    // Regression for 2026-09-17 classifier diff: Astral Ward armor skins moved from
    // Secrets of the Obscure > The Wizard's Tower to Secrets of the Obscure > Skywatch Archipelago.
    // Lyhr sells the Astral Ward armor only in Outer Ring (The Wizard's Tower),
    // while his NPC infobox lists both Droknar's Light (Skywatch Archipelago) and Outer Ring.
    [Theory]
    [InlineData("Astral Ward Heavy Boots (skin)")]
    [InlineData("Astral Ward Light Coat (skin)")]
    public async Task AstralWardArmorShouldReturnWizardsTower(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Secrets of the Obscure", "The Wizard's Tower");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    // Regression for 2026-09-17 classifier diff: Serpent's Wrath / Polychromatic unlocks
    // disappeared from Janthir Wilds > Lowland Shore entirely.
    [Theory]
    [InlineData("Serpent's Wrath Warhorn")]
    [InlineData("Polychromatic Heavy Breastplate (skin)")]
    public async Task JanthirWildsUnlocksShouldReturnLowlandShore(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Janthir Wilds", "Lowland Shore");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task StellarWeaponsShouldReturnDomainOfIstan()
    {
        var unlockName = "Stellar Cleaver";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 4", "Domain of Istan");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldInGemStoreThenShouldReturnGemStore()
    {
        var unlockName = "Aurene's Crystalline Claws (heavy skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Gem Store");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldInWizardsVaultThenShouldReturnWizardsVault()
    {
        var unlockName = "Tropical Leaf Cape";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Wizard's Vault");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Cobalt Antique Reaver")]
    [InlineData("Terracotta Antique Reaver")]
    [InlineData("Calcite Antique Reaver")]
    [InlineData("Citrine Antique Reaver")]
    [InlineData("Viridian Antique Reaver")]
    public async Task CraftingWeaponsShouldBeLinkedToCrafting(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Crafting");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldByExchangeSpecialistThenShouldPrioritizeGemStoreOverBlackLionExchangeSpecialist()
    {
        var unlockName = "Frying Pan (toy)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Gem Store");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    //Destabilized Magic stuff
    [InlineData("Shiny Pistol (skin)")]
    [InlineData("Shiny Rifle (skin)")]
    [InlineData("Shiny Short Bow (skin)")]
    [InlineData("Shiny Staff (skin)")]
    [InlineData("Shiny Torch (skin)")]
    [InlineData("Ultra Shiny Pistol (skin)")]
    [InlineData("Ultra Shiny Rifle (skin)")]
    [InlineData("Ultra Shiny Short Bow (skin)")]
    [InlineData("Ultra Shiny Staff (skin)")]
    [InlineData("Ultra Shiny Torch (skin)")]
    // Bag of Jewels used to link to guild commendation
    [InlineData("Goblet of Kings (skin)")]
    public async Task IgnoreCertainItemsBecauseMysticForgeStuffShouldBeMysticForge(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Mystic Forge");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Bolt (skin)")]
    [InlineData("Frostfang (skin)")]
    [InlineData("Tooth of Frostfang (skin)")]
    [InlineData("Corrupted Skeggox")]
    [InlineData("Tooth of Frostfang Experiment (skin)")]
    public async Task LegendaryShouldBeLegendary(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Legendary");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }


    [Theory]
    [InlineData("Wintergreen Dagger", "Core", "Secret Lair of the Snowmen")]  //skin
    [InlineData("Aetherized Indigo Staff", "Core", "Old Lion's Court")] //skin
    [InlineData("Mini Vermilion Assault Knight", "Core",  "Old Lion's Court")]
    [InlineData("Assaulter's Sparking Dagger (skin)", "Heart of Thorns", "Spirit Vale")]
    public async Task RaidShouldBeRaidCategory(string unlockName, string raidGroupName, string raidName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Raids", raidGroupName, raidName);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Living Water Axe (skin)", "End of Dragons")]
    [InlineData("Envy's Bite (skin)", "Secrets of the Obscure")]
    public async Task RaidShouldBeRaidGroup(string unlockName, string raidGroupName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = Find(results, "Raids", raidGroupName);
        var unlock = group.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }



    [Theory]
    [InlineData("Empowered Boneskinner's Spine (skin)")]
    [InlineData("Empowered Boneskinner's Rib (skin)")]
    [InlineData("Boneskinner's Totem (skin)")]
    [InlineData("Empowered Boneskinner's Totem (skin)")]
    public async Task BoneskinnerItemsShouldBeBoneSkinner(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Raids", "Icebrood Saga", "Boneskinner");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Iron Legion Flamesaw (skin)")]
    public async Task IceBroodSagaShouldBeIceBroodSaga(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Cities", "Eye of the North");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task IceBroodSagaMiniWithAchiShouldBeIceBroodSaga()
    {
        var achiName = "Visions of the Past: Steel and Fire (achievements)#achievement5188"; //Minis of Steel
        var unlockName = "Mini Ryland Steelcatcher";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, achiName, unlockName);
        var category = Find(results, "Cities", "Eye of the North");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);
        var achi = category.Unlocks.Single(c => c.Name == achiName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.NotNull(achi);
        Assert.NotNull(achi.ApiData);
    }

    [Fact]
    public async Task ItemsReferenceByASetShouldBeLinkedCorrectly()
    {
        var unlockName = "Skyforged Axe";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = Find(results, "Expansions & Living World", "Secrets of the Obscure");
        var unlock = group.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Endless Guild Banner Tonic")]
    [InlineData("Guild Broad Axe (skin)")]
    [InlineData("Shimmering Axe (skin)")]
    [InlineData("Tenebrous Axe (skin)")]
    public async Task GuildUnlocksShouldBeLinkedToGuilds(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Guild");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Unbreakable Choir Bell")]
    [InlineData("Candy Cane Axe")]
    public async Task WintersdayUnlocksShouldBeLinkedToWintersday(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Festivals", "Wintersday");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Cobalt Antique Artifact")]
    [InlineData("Illustrious Breastplate")]
    public async Task AscendedCraftingSkinsShouldBeCrafting(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Crafting");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldForBlackLionStatuetteAndHistoricalInGemStoreThenShouldPrioritizeBlackLionStatuetteOverGemStore()
    {
        var unlockName = "Aetherblade Heavy Warhelm";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Black Lion Statuette");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }
    
    [Fact]
    public async Task GivenUnlockSoldByVendorForTokenInZoneWhenClassifyingUnlockThenShouldReturnZone()
    {
        var unlockName = "Endless Spotted Beetle Tonic";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Dragon's Stand");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSoldByVendorInZoneForCommonCurrencyWhenClassifyingUnlockThenShouldReturnZone()
    {
        var unlockName = "Mini Whisper of Jormag";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Icebrood Saga", "Bjora Marches");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenBloodRubyBackpackSoldInBloodstoneFenShouldLinkToCorrectCategory()
    {
        var unlockName = "Blood Ruby Backpack (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 3", "Bloodstone Fen");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Heavy Fused Gauntlets (skin)")]
    [InlineData("Wheel of the Lion's Champion (skin)")]
    public async Task GivenLwS1RewardShouldBeLwS1(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 1", "Season 1");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Ad Infinitum (skin)", "Mystic Forge")]
    [InlineData("Unbound (skin)", "Crafting")]
    [InlineData("Upper Bound (skin)", "Crafting")]
    [InlineData("Finite Result (skin)", "Crafting")]
    public async Task GivenItemsWithRareEssenceofLuckShouldIgnoreIt(string unlockName, string expectedCategory)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", expectedCategory);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Painter's Brilliance Axe")] // skin
    [InlineData("Abaddon Axe (skin)")]
    [InlineData("Collapsing Star Spear")] //skin
    [InlineData("Chiroptophobia")] //skin
    public async Task GivenSkinSoldForBlackLionClaimTicketShouldLinkToBlackLionClaimTicket(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Black Lion Claim Ticket");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Golden Talon")] // skin
    [InlineData("Fuzzy Leopard Hat (heavy skin)")]
    public async Task GivenSkinSoldForBlackLionStatuettesShouldLinkToBlackLionStatuettes(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Black Lion Statuette");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Mini Dolyak")]
    public async Task MiniDolyakShouldLinkToWvW(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "PvP / WvW", "WvW");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Peacemaker's Axe (skin)", "Rata Sum")]
    [InlineData("Peacemaker's Dagger (skin)", "Rata Sum")]
    [InlineData("Aureate Sconce (skin)", "Divinity's Reach")]
    [InlineData("Aureate Spear (skin)", "Divinity's Reach")]
    public async Task CulturalWeaponsForCityShouldLinkToRespectiveCity(string unlockName, string city)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Cities", city);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Inquest Axe (skin)", "Crucible of Eternity")]
    [InlineData("Inquest Dagger (skin)", "Crucible of Eternity")]
    [InlineData("Arms of Koda (skin)", "Honor of the Waves")]
    [InlineData("Flame Legion Boots (skin)", "Citadel of Flame")]
    [InlineData("Nightmare Court Armguards (skin)", "Twilight Arbor")]
    [InlineData("Forgeman Mantle (skin)", "Sorrow's Embrace")]
    public async Task CulturalWeaponsForDungeonShouldLinkToRespectiveDungeon(string unlockName, string dungeonName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Dungeons", dungeonName);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Theory]
    [InlineData("Peacemaker's Javelin")] // cities
    [InlineData("Inquest Bident (skin)")] // dungeon
    [InlineData("Tribal Bident")] // set
    public async Task CulturalWeaponsSpearsShouldLinkToCastora(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Visions of Eternity", "Shipwreck Strand");
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
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", zoneName);
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task NoveltiesWork()
    {
        var unlockName = "Endless Exalted Caster Tonic";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal(186, unlock.ApiData.Id);
        Assert.Equal(Type.Novelty, unlock.ApiData.Type);
        Assert.Equal("Endless Exalted Caster Tonic", unlock.ApiData.Name);
        Assert.NotNull(unlock.ApiData.IconUrl);
        Assert.NotNull(unlock.ApiData.IconSheet);
        Assert.NotNull(unlock.ApiData.IconX);
        Assert.NotNull(unlock.ApiData.IconY);
        Assert.Equal(76174, unlock.ApiData.ChatCodeId);
    }

    [Fact]
    public async Task BlueChoyaKiteIsCrystalOasis()
    {
        var unlockName = "Blue Choya Kite";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Path of Fire", "Crystal Oasis");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task MistShardVisageIsDragonFall()
    {
        var unlockName = "Mist Shard Visage";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 4", "Dragonfall");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task FuneraryAxeSkinIsDesertHighlands()
    {
        var unlockName = "Funerary Axe (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Path of Fire", "Desert Highlands");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal(7422, unlock.ApiData.Id);
        Assert.Equal("Funerary Axe", unlock.ApiData.Name);
        Assert.NotNull(unlock.ApiData.IconUrl);
        Assert.NotNull(unlock.ApiData.IconSheet);
        Assert.NotNull(unlock.ApiData.IconX);
        Assert.NotNull(unlock.ApiData.IconY);
        Assert.Equal(7422, unlock.ApiData.Id);
    }

    [Fact]
    public async Task GivenUnlockHavingRecipeWithTokenAsIngredientShouldLinkToCategory()
    {
        var unlockName = "Mini Foostivoo the Merry";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Festivals", "Wintersday");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);
        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockHavingSoldForFestivalCurrencyShouldLinkToFestivalCategory()
    {
        var unlockName = "Plush Zhaia Backpack (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Festivals", "Halloween");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);
        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockContainsExoticChestShouldBeGeneralCategory()
    {
        var unlockName = "Adam (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "General");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenUnlockSkinShouldContainApiData()
    {
        var unlockName = "Bladed Helmet (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, "Bladed Helmet (skin)");
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Verdant Brink");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }


    [Fact]
    public async Task CraftingShouldBeCrafting()
    {
        string unlockName = "Leather Coat (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Crafting");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task ItemInMultipleClassificationsShouldUseMostCommon()
    {
        string unlockName = "Sunspear Warsickle (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var group = Find(results, "Expansions & Living World", "Path of Fire");
        var unlock = group.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task MiniWithTypePropertyCaptalizedAndMultipleCostsShouldStillFindApiDataAndTakeVendorWithHighestMathcingCosts()
    {
        string unlockName = "Mini Tyrannus Maneater";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Visions of Eternity", "Starlit Weald");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }


    [Theory]
    // used to do this: Tome of the Rubicon (skin) -> Tome of the Rubicon -> Piles of Bloodstone Dust -> Grand Exalted Chest -> Auric Basin
    [InlineData("Tome of the Rubicon (skin)")] //don't follow piles of bloodstone shard!
    [InlineData("Abyssal Scepter (skin)")]
    public async Task GivenItemWithRecipeSourceMysticForgeShouldBeCategoryMysticForge(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Other", "Mystic Forge");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementWithRewardShouldTakeGroupAndCategoryLinkedToAchievementCategory()
    {
        var unlockSkinName = "Temple Gate (skin)";
        var unlockAchiName = "Seitung Province (achievements)#achievement6331";
        var unlocks = new string[] { unlockSkinName, unlockAchiName };
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlocks);
        var category = Find(results, "Expansions & Living World", "End of Dragons", "Seitung Province");
        var unlockSkin = category.Unlocks.Single(c => c.Name == unlockSkinName);
        var unlockAchi = category.Unlocks.Single(c => c.Name == unlockAchiName);

        Assert.NotNull(unlockSkin);
        Assert.NotNull(unlockSkin.ApiData);

        Assert.NotNull(unlockAchi);
        Assert.NotNull(unlockAchi.ApiData);
    }

    [Fact]
    public async Task ItemsRequiredForAchievementShouldCategorizeCorrectly2()
    {
        var unlocksInMf = new List<string> {
            "Icy Dragon Sword (skin)",
            "Jormag's Needle(skin)",
        };
        var unlocksCrafting= new List<string> {
            "Corrupted Shard (skin)",
            "Corrupted Artifact",
            "Corrupted Avenger",
            "Corrupted Sledgehammer",
            "Corrupted Harpoon Gun",
            "Corrupted Greatbow",
            "Corrupted Cudgel",
            "Corrupted Revolver",
            "Corrupted Blaster",
        };
        var unlocksLegendary = new List<string> { 
            "Corrupted Skeggox" // legendary
        };
        var unlockAchiName = "Rare Collections#achievement1744";
        var unlocks =  unlocksLegendary.Append(unlockAchiName).Concat(unlocksInMf).Concat(unlocksCrafting).ToArray();
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlocks);
        var category = Find(results, "Other", "Crafting");
        var unlockAchi = category.Unlocks.Single(c => c.Name == unlockAchiName);

        Assert.NotNull(unlockAchi);
        Assert.NotNull(unlockAchi.ApiData);
    }

    [Theory]
    [InlineData("Auric Axe (skin)")]
    [InlineData("Auric Longbow (skin)")]
    public async Task ItemsRequiredForAchievementShouldCategorizeCorrectly(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementWithRewardThenItemsRequiredForAchievementShouldInfluenceGroupAndCategory()
    {
        var unlocksForAchi = new List<string> { "Auric Axe (skin)", "Auric Longbow (skin)" };
        var unlockAchiName = "Basic Collections#achievement2262"; // Auric Weapons achievement
        var unlockRewardName = "Auric Backplate (skin)";
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, [.. unlocksForAchi, unlockAchiName]);
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Auric Basin");
        var unlockAchi = category.Unlocks.Single(c => c.Name == unlockAchiName);
        var unlockReward = category.Unlocks.Single(c => c.Name == unlockRewardName);

        Assert.NotNull(unlockAchi);
        Assert.NotNull(unlockAchi.ApiData);

        Assert.NotNull(unlockReward);
        Assert.NotNull(unlockReward.ApiData);
    }

    [Fact]
    public async Task GivenAchievementWithoutRewardThenItemsRequiredForAchievementShouldInfluenceGroupAndCategory()
    {
        var unlocksForAchi = new List<string> { "Bladed Greaves (skin)" };
        var unlockName = "Basic Collections#achievement2407"; // Bladed Armor achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, [.. unlocksForAchi, unlockName]);
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Verdant Brink");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal("https://render.guildwars2.com/file/109A0AE76FCA3EBC03039BA668B90142CAB0DDA2/866109.png", unlock.ApiData.IconUrl?.ToString());
    }

    [Theory(Skip = "not sure yet")]
    [InlineData("New Kaineng City (achievements)#achievement6213")] // "Protector of Kaineng", repeatable, ideally I want this still!
    [InlineData("New Year's Customs#achievement6063")] // "(Weekly) Lunar Festivities", weekly
    [InlineData("New Year's Customs#achievement4080")] // "(Annual) New Year's Resolution", contains "{Annual}"
    [InlineData("Super Adventure Box: Nostalgia#achievement2843")] // "Course Load", repeatable
    public async Task GivenAchievementsThatAreNotRepeatableShouldNotBeLinkedtoUnlockCategory(string unlockName)
    {
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var groups = results.Categories.Where(g => g.Unlocks.Any(u => u.Name == unlockName)).ToList();
        var categories = results.GetAllCategories().Where(c => c.Unlocks.Any(u => u.Name == unlockName)).ToList();

        Assert.Empty(groups);
        Assert.Empty(categories);
    }

    [Fact]
    public async Task GivenAchievementsPartOfAchievementCategoryWhichIsLinkedtoUnlockCategory()
    {
        var unlockName = "Auric Basin (achievements)#achievement2292"; // Highest Gear achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "Heart of Thorns", "Auric Basin");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
    }

    [Fact]
    public async Task GivenAchievementsRewardsTitleApiDataShouldContainAchievementAndTitle()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3221"; // Playing Chicken  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 3", "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal(3221, unlock.ApiData.Id);
        Assert.Equal(Type.Achievement, unlock.ApiData.Type);
        Assert.Equal("Playing Chicken", unlock.ApiData.Name);
        Assert.Equal("Push a chicken to its limits.", unlock.ApiData.Requirement);
        Assert.Equal("Chicken Chaser", unlock.ApiData.RewardName);
        Assert.Equal(new Uri("/img/icon_title.png", UriKind.Relative), unlock.ApiData.RewardIconUrl);

    }

    [Fact]
    public async Task GivenAchievementsRewardsMasteryPointThenApiDataShouldContainRewardIconInApiData()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3214"; // Quirky Quaggan Quest  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 3", "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal(new Uri("/img/mastery_Maguuma.png", UriKind.Relative), unlock.ApiData.RewardIconUrl);
    }

    [Fact]
    public async Task GivenAchievementsRewardsItemThenApiDataShouldContainRewardIconInApiData()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3188"; // Stay Unfrosty  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 3", "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal("Magic-Warped Packet", unlock.ApiData.RewardName);
        Assert.Equal(new Uri("https://render.guildwars2.com/file/C399F9556A9478EF32A491345C4DA07605AD49D6/1465576.png"), unlock.ApiData.RewardIconUrl);
    }

    [Fact]
    public async Task GivenAchievementsThenApiDataShouldContainIconInApiData()
    {
        var unlockName = "A Crack in the Ice (achievements)#achievement3188"; // Stay Unfrosty  achievement
        var results = await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken, unlockName);
        var category = Find(results, "Expansions & Living World", "LW Season 3", "Bitterfrost Frontier");
        var unlock = category.Unlocks.Single(c => c.Name == unlockName);

        Assert.NotNull(unlock);
        Assert.NotNull(unlock.ApiData);
        Assert.Equal(new Uri("https://render.guildwars2.com/file/136E663C59275A077ADD394C935F26091B065A51/1601931.png"), unlock.ApiData.IconUrl);
        Assert.NotNull(unlock.ApiData.IconSheet);
        Assert.NotNull(unlock.ApiData.IconX);
        Assert.NotNull(unlock.ApiData.IconY);
    }
}
