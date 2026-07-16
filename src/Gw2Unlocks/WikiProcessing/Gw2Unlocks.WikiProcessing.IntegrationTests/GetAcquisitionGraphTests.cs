using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki;
using Gw2Unlocks.WikiProcessing.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.WikiProcessing.IntegrationTests;

public class GetAcquisitionGraphTests : ServiceProviderBasedTest<IGw2WikiProcessingSource>
{
    private readonly Gw2WikiIntegrationTestSuccessResponseFake fakeWikiApi;

    public GetAcquisitionGraphTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        fakeWikiApi = (Gw2WikiIntegrationTestSuccessResponseFake)GetService<IGw2WikiCache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddWikiGraphSource()
                .AddCacheDir(() => ".")
                .AddSingleton<Gw2WikiIntegrationTestSuccessResponseFake>()
                .AddSingleton<IGw2WikiCache>(sp => sp.GetRequiredService<Gw2WikiIntegrationTestSuccessResponseFake>());
    }

    [Fact]
    public async Task HistoricalItemsShouldNotBeIncludedInTheGraph()
    {
        SetFile("historical");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public async Task NamedExoticWeaponsShouldBeTaggedAsSuch()
    {
        SetFile("named_exotic_weapons");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);
        const string weapon = "Anura";
        var weaponNode = graph.GetNode(weapon, NodeType.Weapon);

        Assert.NotNull(weaponNode);

        Assert.Equal("true", weaponNode.Metadata["IsNamedExoticWeapon"]);
    }

    [Theory]
    [InlineData("Mini Gorseval the Multifarious", "Gorseval the Multifarious", NodeType.NPC)]
    [InlineData("Gyala Delve: Hero's Choice Chest", "Destroy the ravenous wanderer", NodeType.Event)]
    public async Task RewardItemShouldLinkToRewardSource(string reward, string rewardSource, NodeType sourceNodeType)
    {
        SetFile("rewards");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);
        var rewardNode = graph.GetNode(reward, NodeType.Item);
        var rewardSourceNode = graph.GetNode(rewardSource, sourceNodeType);

        Assert.NotNull(rewardNode);
        Assert.NotNull(rewardSourceNode);

        Assert.Contains(graph.Edges, e => e.From == reward && e.To == rewardSource && e.Type == EdgeType.RewardedBy);
    }

    [Theory]
    [InlineData("Catalyst's Stamp", "Defeat Renyak!", NodeType.Event)]
    public async Task RewardItemWhenProfessionRequirementShouldNotLinkToRewardSource(string reward, string rewardSource, NodeType sourceNodeType)
    {
        SetFile("rewards_exclusions");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);
        var rewardNode = graph.GetNode(reward, NodeType.Item);
        var rewardSourceNode = graph.GetNode(rewardSource, sourceNodeType);

        Assert.NotNull(rewardNode);
        Assert.NotNull(rewardSourceNode);

        Assert.DoesNotContain(graph.Edges, e => e.From == reward && e.To == rewardSource && e.Type == EdgeType.RewardedBy);
    }

    [Fact]
    public async Task MiniDolyakShouldBeSoldBeSkirmishSupervisorWhichIsLocatedInWorldvsWorld()
    {
        SetFile("Mini_Dolyak");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string item = "Mini Dolyak";
        var itemNode = graph.GetNode(item, NodeType.Item);
        const string vendor = "Skirmish Supervisor";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);
        const string region = "World vs. World";
        var regionNode = graph.GetNode(region, NodeType.Location);

        Assert.NotNull(itemNode);
        Assert.NotNull(vendorNode);
        Assert.NotNull(regionNode);

        Assert.Contains(graph.Edges, e => e.From == item && e.To == vendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.ContainsKey("cost"));
        Assert.Contains(graph.Edges, e => e.From == vendor && e.To == region && e.Type == EdgeType.LocatedIn);
    }
    
    [Fact]
    public async Task HomesteadUnlocksShouldLinkToDeftLahar()
    {
        SetFile("homestead");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);
        const string armor = "Polychromatic Heavy Breastplate";
        var armorNode = graph.GetNode(armor, NodeType.Armor);
        const string container = "Polychromatic Coat Box";
        var containerNode = graph.GetNode(container, NodeType.Item);
        const string vendor = "Deft Lahar";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);

        Assert.NotNull(armorNode);
        Assert.NotNull(containerNode);
        Assert.NotNull(vendorNode);

        Assert.Contains(graph.Edges, e => e.From == armor && e.To == container && e.Type == EdgeType.ContainedIn);
        Assert.Contains(graph.Edges, e => e.From == container && e.To == vendor && e.Type == EdgeType.SoldBy
                && e.Metadata != null && e.Metadata.ContainsKey("cost"));
    }

    [Fact]
    public async Task AchievementsShouldBeParsedProperly()
    {
        SetFile("achievements");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);
        const int highestGearAchiId = 2292;
        var highestGearNode = graph.GetAchievementNode(highestGearAchiId);
        const int auricWeaponsAchiId = 2262;
        var auricWeaponsNode = graph.GetAchievementNode(auricWeaponsAchiId);

        Assert.NotNull(highestGearNode);
        Assert.Equal("2292", highestGearNode.Metadata["achievementId"]);
        Assert.Equal("Highest Gear", highestGearNode.Metadata["name"]);
        Assert.Equal("Auric Basin (achievements)", highestGearNode.Metadata["category"]);
        Assert.NotNull(auricWeaponsNode);
        Assert.Equal("2262", auricWeaponsNode.Metadata["achievementId"]);
        Assert.Equal("Auric Weapons", auricWeaponsNode.Metadata["name"]);
        Assert.Equal("Basic Collections", auricWeaponsNode.Metadata["category"]);
    }

    [Fact]
    public async Task MultipleSkinUnlocksShouldBeSplitProperly()
    {
        SetFile("skin_multiple_unlock");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string item = "Acclaimed Militia Chestpiece Skin";
        var itemNode = graph.GetNode(item, NodeType.Item);
        const string skinHeavy = "Acclaimed Militia Chestpiece (heavy skin)";
        var skinHeavyNode = graph.GetNode(skinHeavy, NodeType.Skin);
        const string skinMedium = "Acclaimed Militia Chestpiece (medium skin)";
        var skinMediumNode = graph.GetNode(skinMedium, NodeType.Skin);
        const string skinLight = "Acclaimed Militia Chestpiece (light skin)";
        var skinLightNode = graph.GetNode(skinLight, NodeType.Skin);

        Assert.NotNull(itemNode);
        Assert.NotNull(skinHeavyNode);
        Assert.NotNull(skinMediumNode);
        Assert.NotNull(skinLightNode);

        Assert.Contains(graph.Edges, e => e.From == skinHeavy && e.To == item && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == skinMedium && e.To == item && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == skinLight && e.To == item && e.Type == EdgeType.SkinUnlock);
    }

    [Fact]
    public async Task CraftingRecipeShouldLookAtIngredientsAndObjectShouldGoToZone()
    {
        SetFile("AB_map");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string skin = "Auric Axe (skin)";
        var skinNode = graph.GetNode(skin, NodeType.Skin);
        const string weapon = "Auric Axe";
        var weaponNode = graph.GetNode(weapon, NodeType.Weapon);
        const string itemCore = "Exalted Axe Core";
        var itemCoreNode = graph.GetNode(itemCore, NodeType.Item);
        const string itemIngot = "Auric Ingot";
        var itemIngotNode = graph.GetNode(itemIngot, NodeType.Item);
        const string chest = "Exalted Chest";
        var chestNode = graph.GetNode(chest, NodeType.Gw2Object);
        const string zone = "Auric Basin";
        var zoneNode = graph.GetNode(zone, NodeType.Location);

        Assert.NotNull(skinNode);
        Assert.NotNull(weaponNode);
        Assert.NotNull(itemCoreNode);
        Assert.NotNull(itemIngotNode);
        Assert.NotNull(chestNode);
        Assert.NotNull(zoneNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == weapon && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == weapon && e.To == itemCore && e.Type == EdgeType.HasIngredient
                            && e.Metadata != null && e.Metadata.TryGetValue("discipline", out var discipline) && discipline == "weaponsmith");
        Assert.Contains(graph.Edges, e => e.From == itemCore && e.To == itemIngot && e.Type == EdgeType.HasIngredient
                            && e.Metadata != null && e.Metadata.TryGetValue("source", out var source) && source == "recipe sheet");
        Assert.Contains(graph.Edges, e => e.From == itemIngot && e.To == chest && e.Type == EdgeType.GatheredFrom);
        Assert.Contains(graph.Edges, e => e.From == chest && e.To == zone && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task SoldByShouldReturnVendorWithLocation()
    {
        SetFile("AB_map");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string item = "Mini Exalted Sage";
        var itemNode = graph.GetNode(item, NodeType.Item);
        const string vendor = "Exalted Mastery Vendor";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);
        const string area = "Tarir, the Forgotten City";
        var areaNode = graph.GetNode(area, NodeType.Location);
        const string zone = "Auric Basin";
        var zoneNode = graph.GetNode(zone, NodeType.Location);

        Assert.NotNull(itemNode);
        Assert.NotNull(vendorNode);
        Assert.NotNull(areaNode);
        Assert.NotNull(zoneNode);

        Assert.Contains(graph.Edges, e => e.From == item && e.To == vendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.ContainsKey("cost"));
        Assert.Contains(graph.Edges, e => e.From == vendor && e.To == area && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area && e.To == zone && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task GivenVendorContainsLocationTagSoldByShouldOnlyReturnVendorWithSpecificLocation()
    {
        SetFile("VendorSpecificLocation");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string item = "Mini Awakened Archer";
        var itemNode = graph.GetNode(item, NodeType.Item);
        const string vendor = "Awakened Merchant";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);
        const string area = "Free City of Amnoon";
        var areaNode = graph.GetNode(area, NodeType.Location);
        const string zone = "Crystal Oasis";
        var zoneNode = graph.GetNode(zone, NodeType.Location);

        Assert.NotNull(itemNode);
        Assert.NotNull(vendorNode);
        Assert.NotNull(areaNode);
        Assert.NotNull(zoneNode);

        Assert.Contains(graph.Edges, e => e.From == item && e.To == vendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.ContainsKey("cost"));
        Assert.Contains(graph.Edges, e => e.From == vendor && e.To == area && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area && e.To == zone && e.Type == EdgeType.LocatedIn);
        // for selling purposes, the vendor should only be located in the specific area, not in all areas
        Assert.Single(graph.Edges, e => e.From == vendor && e.Type == EdgeType.LocatedIn);
    }

    [Theory]
    [InlineData("Lumps of Aurillium")]
    [InlineData("Piles of Bloodstone Dust")]
    public async Task RedirectShouldResolveToActualItem(string redirect)
    {
        SetFile("redirects");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        var actualNode = graph.GetOrCreate(redirect);

        Assert.NotNull(actualNode);
        Assert.NotEqual(NodeType.None, actualNode.Type);
        Assert.DoesNotContain(graph.Nodes, kvp => kvp.Key == redirect);
    }

    [Fact]
    public async Task ShouldFindSoldbyBFmap()
    {
        SetFile("BF_map");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string skin = "Blood Ruby Backpack (skin)";
        var skinNode = graph.GetNode(skin, NodeType.Skin);
        const string item = "Blood Ruby Backpack";
        var itemNode = graph.GetNode(item, NodeType.BackItem);
        const string vendor = "Scholar Rakka";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);
        const string area = "Haunted Canyons";
        var areaNode = graph.GetNode(area, NodeType.Location);
        const string zone = "Bloodstone Fen";
        var zoneNode = graph.GetNode(zone, NodeType.Location);

        Assert.NotNull(skinNode);
        Assert.NotNull(itemNode);
        Assert.NotNull(vendorNode);
        Assert.NotNull(areaNode);
        Assert.NotNull(zoneNode);

        Assert.Contains(graph.Edges, e => e.From == item && e.To == vendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.ContainsKey("cost"));
        Assert.Contains(graph.Edges, e => e.From == vendor && e.To == area && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area && e.To == zone && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task ContainedInShouldRecurseToVendor()
    {
        SetFile("Arah_contains");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string item = "Axe of the Dragon's Deep";
        var itemNode = graph.GetNode(item, NodeType.Weapon);
        const string containerArah = "Arah Weapons Box";
        var containerArahNode = graph.GetNode(containerArah, NodeType.Item);
        const string containerDungeon = "Dungeon Weapon Container";
        var containerDungeonNode = graph.GetNode(containerDungeon, NodeType.Item);

        Assert.NotNull(itemNode);
        Assert.NotNull(containerArahNode);
        Assert.NotNull(containerDungeonNode);
        
        Assert.Contains(graph.Edges, e => e.From == item && e.To == containerArah && e.Type == EdgeType.ContainedIn);
        Assert.Contains(graph.Edges, e => e.From == containerArah && e.To == containerDungeon && e.Type == EdgeType.ContainedIn);
    }


    [Fact]
    public async Task SkinShouldRecurseThroughItems()
    {
        SetFile("LA_halloween");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string skin = "Plush Zhaia Backpack (skin)";
        var skinNode = graph.GetNode(skin, NodeType.Skin);
        const string item = "Plush Zhaia Backpack";
        var itemNode = graph.GetNode(item, NodeType.BackItem);
        const string vendor = "Halloween Vendor";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);
        const string area = "Hooligan's Route";
        var areaNode = graph.GetNode(area, NodeType.Location);
        const string city = "Lion's Arch";
        var cityNode = graph.GetNode(city, NodeType.Location);

        Assert.NotNull(skinNode);
        Assert.Equal("13255", skinNode.Metadata!["id"]);
        Assert.NotNull(itemNode);
        Assert.NotNull(vendorNode);
        Assert.NotNull(areaNode);
        Assert.NotNull(cityNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == item && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == item && e.To == vendor && e.Type == EdgeType.SoldBy);
        Assert.Contains(graph.Edges, e => e.From == vendor && e.To == area && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area && e.To == city && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task GemStoreDataShouldShowSoldByGemStoreVendor()
    {
        SetFile("gem_store_data");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string skin = "Aurene's Crystalline Claws (heavy skin)";
        var skinNode = graph.GetNode(skin, NodeType.Skin);
        const string item = "Aurene's Crystalline Claws Skin";
        var itemNode = graph.GetNode(item, NodeType.Item);
        const string containerPack = "Aurene’s Champion Pack";
        var containerPackNode = graph.GetNode(containerPack, NodeType.GemStoreCombo);
        const string gemStoreVendor = "Gem Store";
        var gemStoreVendorNode = graph.GetNode(gemStoreVendor, NodeType.NPC);


        Assert.NotNull(skinNode);
        Assert.NotNull(itemNode);
        Assert.NotNull(containerPackNode);
        Assert.NotNull(gemStoreVendorNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == item && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == item && e.To == containerPack && e.Type == EdgeType.ContainedIn);

        Assert.Contains(graph.Edges, e => e.From == item && e.To == gemStoreVendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.TryGetValue("cost", out string? costItem) && costItem.Contains("500 Gems", System.StringComparison.Ordinal));
        Assert.Contains(graph.Edges, e => e.From == containerPack && e.To == gemStoreVendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.TryGetValue("cost", out string? costItem) && costItem.Contains("1350 Gems", System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task GemStoreDataWhenAvailabilityIsHistoricalShouldShowSoldByGemStoreVendorWithHistocalAvailability()
    {
        SetFile("gem_store_data");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string skin = "Aetherblade Heavy Warhelm";
        var skinNode = graph.GetNode(skin, NodeType.Skin);
        const string item = "Aetherblade Heavy Warhelm Skin";
        var itemNode = graph.GetNode(item, NodeType.Item);
        const string container = "Aetherblade Heavy Armor Skin";
        var containerNode = graph.GetNode(container, NodeType.Item);
        const string gemStoreVendor = "Gem Store";
        var gemStoreVendorNode = graph.GetNode(gemStoreVendor, NodeType.NPC);


        Assert.NotNull(skinNode);
        Assert.NotNull(itemNode);
        Assert.NotNull(containerNode);
        Assert.NotNull(gemStoreVendorNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == item && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == item && e.To == container && e.Type == EdgeType.ContainedIn);
        Assert.Contains(graph.Edges, e => e.From == container && e.To == gemStoreVendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null 
                        && e.Metadata.TryGetValue("cost", out string? costItem) && costItem.Contains("800 Gems", System.StringComparison.Ordinal)
                        && e.Metadata.TryGetValue("availability", out string? avaiabilityItem) && avaiabilityItem.Contains("historical", System.StringComparison.Ordinal)
                        );
    }

    [Theory]
    [InlineData("Magi's Skyforged Axe", NodeType.Weapon, "Skyforged weapons")]
    [InlineData("Sinister Saryx Axe", NodeType.Weapon, "Saryx weapons")]
    [InlineData("Ritualist's Ancient Canthan Light Shoulderpads", NodeType.Armor, "Ancient Canthan armor")]
    [InlineData("Ritualist's Ancient Canthan Medium Shoulderpads", NodeType.Armor, "Ancient Canthan armor")]
    [InlineData("Illustrious Breastplate", NodeType.Skin, "Illustrious armor")]
    [InlineData("Illustrious Doublet", NodeType.Skin, "Illustrious armor")]
    public async Task SetsShouldWork(string item, NodeType itemType, string set)
    {
        SetFile("sets");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        var itemNode = graph.GetNode(item, itemType);
        Assert.NotNull(itemNode);

        var setNode = graph.GetNode(set, NodeType.Set);
        Assert.NotNull(setNode);

        Assert.Contains(graph.Edges, e => e.From == item && e.To == set && e.Type == EdgeType.ContainedIn);
    }

    [Fact]
    public async Task SetsAndRedirectsShouldNotHaveInfiniteLoops()
    {
        string item = "Flamewalker armor";
        SetFile("sets_infinite_loop");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        var itemNode = graph.GetNode(item, NodeType.Set);
        Assert.NotNull(itemNode);
    }

    [Theory]
    [InlineData("Chest (Secrets of the Obscure)", NodeType.Gw2Object, "Skyforged weapons")]
    [InlineData("Weekly Quickplay Raid Cache", NodeType.Item, "Ancient Canthan armor")]

    public async Task SetContainsShouldWork(string chestOrContainer, NodeType chestOrContainerType, string set)
    {
        SetFile("set_contains");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        var chestOrContainerNode = graph.GetNode(chestOrContainer, chestOrContainerType);
        Assert.NotNull(chestOrContainerNode);

        var setNode = graph.GetNode(set, NodeType.Set);
        Assert.NotNull(setNode);

        Assert.Contains(graph.Edges, e => e.From == set && e.To == chestOrContainer && e.Type == EdgeType.ContainedIn);
    }

    [Theory]
    [InlineData("Gwyl Monarch's Selection Box")]
    [InlineData("Ward Hunter's Shoulder Skin")]
    [InlineData("Tropical Leaf Cape Set")]
    [InlineData("Toxinshell Selection Box")]
    public async Task WizardsVaultItemsShouldShowSoldByWizardsVault(string skin)
    {
        SetFile("WizardsVault");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string wizardsVaultVendor = "Wizard's Vault";
        var blackLionWeaponsVendorNode = graph.GetNode(wizardsVaultVendor, NodeType.NPC);
        Assert.NotNull(blackLionWeaponsVendorNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == wizardsVaultVendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.TryGetValue("cost", out string? costItem) && costItem.Contains("Astral Acclaim", System.StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Collapsing Star Spear Skin")]
    [InlineData("Chiroptophobia Greatsword Skin")]
    [InlineData("Painter's Brilliance Axe Skin")]
    [InlineData("Abaddon Axe (skin)")]
    public async Task BlackLionClaimTicketItemsShouldShowSoldByBlackLionClaimTicketVendor(string skin)
    {
        SetFile("Black_Lion_Claim_Ticket_and_Statuette");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string blackLionWeaponsVendor = "Black Lion Weapons Specialist";
        var blackLionWeaponsVendorNode = graph.GetNode(blackLionWeaponsVendor, NodeType.NPC);
        Assert.NotNull(blackLionWeaponsVendorNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == blackLionWeaponsVendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.TryGetValue("cost", out string? costItem) && costItem.Contains("Black Lion Claim Ticket", System.StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Golden Talon Longbow")]
    [InlineData("Fuzzy Leopard Hat")]
    public async Task BlackLionStatuetteItemsShouldShowSoldByBlackLionChestMerchantVendor(string skin)
    {
        SetFile("Black_Lion_Claim_Ticket_and_Statuette");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string blackLionChestVendor = "Black Lion Chest Merchant";
        var blackLionWeaponsVendorNode = graph.GetNode(blackLionChestVendor, NodeType.NPC);
        Assert.NotNull(blackLionWeaponsVendorNode);

        Assert.Contains(graph.Edges, e => e.From == skin && e.To == blackLionChestVendor && e.Type == EdgeType.SoldBy
                        && e.Metadata != null && e.Metadata.TryGetValue("cost", out string? costItem) && costItem.Contains("Black Lion Statuette", System.StringComparison.Ordinal));
    }

    // the page "Free City of Amnoon" contains "redirected [[Elon River]]" which triggered the redirect logic.
    [Fact]
    public async Task CasinoBlitzRewardCashierLocatedInCrystalOasis()
    {
        SetFile("Crystal_Oasis");
        var graph = await GetSut().GetAcquisitionGraph(TestContext.Current.CancellationToken);

        const string vendor = "Casino Blitz Reward Cashier";
        var vendorNode = graph.GetNode(vendor, NodeType.NPC);
        const string area = "Free City of Amnoon";
        var areaNode = graph.GetNode(area, NodeType.Location);
        const string zone = "Crystal Oasis";
        var zoneNode = graph.GetNode(zone, NodeType.Location);

        Assert.NotNull(vendorNode);
        Assert.NotNull(areaNode);
        Assert.NotNull(zoneNode);

        Assert.Contains(graph.Edges, e => e.From == vendor && e.To == area && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area && e.To == zone && e.Type == EdgeType.LocatedIn);
    }

    private void SetFile(string fileName)
    {
        fakeWikiApi.FileName = $"xmlfiles/{fileName}.xml";
    }
}