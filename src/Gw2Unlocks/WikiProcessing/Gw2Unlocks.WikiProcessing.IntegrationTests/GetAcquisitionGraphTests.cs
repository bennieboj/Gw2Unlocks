using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki;
using Gw2Unlocks.WikiProcessing.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.WikiProcessing.IntegrationTests;

public class GetAcquisitionGraphTests : ServiceProviderBasedTest<IGw2WikiGraphSource>
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
    public async Task CraftingRecipeShouldLookAtIngredientsAndObjectShouldGoToZone()
    {
        fakeWikiApi.FileName = "AB_map.xml";
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
        Assert.Contains(graph.Edges, e => e.From == weapon && e.To == itemCore && e.Type == EdgeType.HasIngredient);
        Assert.Contains(graph.Edges, e => e.From == itemCore && e.To == itemIngot && e.Type == EdgeType.HasIngredient);
        Assert.Contains(graph.Edges, e => e.From == itemIngot && e.To == chest && e.Type == EdgeType.GatheredFrom);
        Assert.Contains(graph.Edges, e => e.From == chest && e.To == zone && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task SoldByShouldReturnVendorWithLocation()
    {
        fakeWikiApi.FileName = "AB_map.xml";
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
    public async Task ContainedInShouldRecurseToVendor()
    {
        fakeWikiApi.FileName = "Arah_contains.xml";
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
        fakeWikiApi.FileName = "LA_halloween.xml";
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
}