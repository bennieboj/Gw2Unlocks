using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki.WikiApi;
using Gw2Unlocks.Wiki.WikiApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.Wiki.Implementation.Tests;

public class Gw2WikiSourceTests : ServiceProviderBasedTest<IGw2WikiSource>
{
    private readonly FakeWikiApi fakeWikiApi;

    public Gw2WikiSourceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        fakeWikiApi = (FakeWikiApi)GetService<IWikiApi>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddWikiSource()
                .AddFakeWikiApi();
    }

    private static string CreateSoldByTable(string vendor, string area, string zone)
    {
        return $@"
{{| class=""npc sortable table""
! Vendor
! Area
! Zone
! Cost <nowiki/>
! Notes
|-
| [[{vendor}]]
| [[{area}]]
| [[:{zone}]]
| 1 Gold
| Test
|-
|}}";
    }

    [Fact]
    public async Task SoldByShouldReturnVendorWithLocation()
    {
        fakeWikiApi.AddPage("Mini Exalted Sage", "");
        fakeWikiApi.AddTemplate("{{Sold by|Mini Exalted Sage}}",
            CreateSoldByTable("Exalted Mastery Vendor", "Tarir, the Forgotten City", "Auric Basin"));

        var graph = await GetSut().GetAcquisitionGraph(["Mini Exalted Sage"], null, TestContext.Current.CancellationToken);

        var item = graph.GetNode(NodeType.Item, "Mini Exalted Sage");
        Assert.NotNull(item);
        var vendor = graph.GetNode(NodeType.Vendor, "Exalted Mastery Vendor");
        Assert.NotNull(vendor);
        var area = graph.GetNode(NodeType.Area, "Tarir, the Forgotten City");
        Assert.NotNull(area);
        var zone = graph.GetNode(NodeType.Zone, "Auric Basin");
        Assert.NotNull(zone);

        Assert.Contains(graph.Edges, e => e.From == item.Id && e.To == vendor.Id && e.Type == EdgeType.SoldBy);
        Assert.Contains(graph.Edges, e => e.From == vendor.Id && e.To == area.Id && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area.Id && e.To == zone.Id && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task ContainedInShouldRecurseToVendor()
    {
        fakeWikiApi.AddPage("Item A", "");
        fakeWikiApi.AddPage("Container A", "");
        fakeWikiApi.AddTemplate("{{contained in|Item A}}", "[[Container A]]");
        fakeWikiApi.AddTemplate("{{Sold by|Container A}}",
            CreateSoldByTable("Vendor A", "Tarir, the Forgotten City", "Auric Basin"));

        var graph = await GetSut().GetAcquisitionGraph(["Item A"], null, TestContext.Current.CancellationToken);

        var item = graph.GetNode(NodeType.Item, "Item A");
        var container = graph.GetNode(NodeType.Container, "Container A");
        var vendor = graph.GetNode(NodeType.Vendor, "Vendor A");
        var area = graph.GetNode(NodeType.Area, "Tarir, the Forgotten City");
        var zone = graph.GetNode(NodeType.Zone, "Auric Basin");

        Assert.NotNull(item);
        Assert.NotNull(container);
        Assert.NotNull(vendor);
        Assert.NotNull(area);
        Assert.NotNull(zone);

        Assert.Contains(graph.Edges, e => e.From == item.Id && e.To == container.Id && e.Type == EdgeType.Contains);
        Assert.Contains(graph.Edges, e => e.From == container.Id && e.To == vendor.Id && e.Type == EdgeType.SoldBy);
        Assert.Contains(graph.Edges, e => e.From == vendor.Id && e.To == area.Id && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area.Id && e.To == zone.Id && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task SkinShouldRecurseThroughItems()
    {
        fakeWikiApi.AddPage("Skin X (skin)", "");
        fakeWikiApi.AddPage("Item X", "");
        fakeWikiApi.AddTemplate("{{skin list|Skin X (skin)}}", "[[Item X]]");
        fakeWikiApi.AddTemplate("{{Sold by|Item X}}",
            CreateSoldByTable("Vendor X", "Tarir, the Forgotten City", "Auric Basin"));

        var graph = await GetSut().GetAcquisitionGraph(["Skin X (skin)"], null, TestContext.Current.CancellationToken);

        var skin = graph.GetNode(NodeType.Item, "Skin X (skin)");
        var item = graph.GetNode(NodeType.Item, "Item X");
        var vendor = graph.GetNode(NodeType.Vendor, "Vendor X");
        var area = graph.GetNode(NodeType.Area, "Tarir, the Forgotten City");
        var zone = graph.GetNode(NodeType.Zone, "Auric Basin");

        Assert.NotNull(skin);
        Assert.NotNull(item);
        Assert.NotNull(vendor);
        Assert.NotNull(area);
        Assert.NotNull(zone);

        Assert.Contains(graph.Edges, e => e.From == skin.Id && e.To == item.Id && e.Type == EdgeType.SkinUnlock);
        Assert.Contains(graph.Edges, e => e.From == item.Id && e.To == vendor.Id && e.Type == EdgeType.SoldBy);
        Assert.Contains(graph.Edges, e => e.From == vendor.Id && e.To == area.Id && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area.Id && e.To == zone.Id && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task AchievementShouldStopTraversal()
    {
        fakeWikiApi.AddPage("Skin A", "{{achievement box|Achievement A}}");

        var graph = await GetSut().GetAcquisitionGraph(["Skin A"], null, TestContext.Current.CancellationToken);

        var item = graph.GetNode(NodeType.Item, "Skin A");
        var achievement = graph.GetNode(NodeType.Achievement, "Achievement A");

        Assert.NotNull(item);
        Assert.NotNull(achievement);

        Assert.Contains(graph.Edges, e => e.From == item.Id && e.To == achievement.Id && e.Type == EdgeType.Rewards);
    }

    [Fact]
    public async Task ShouldAvoidInfiniteLoops()
    {
        fakeWikiApi.AddPage("Item Loop", "");
        fakeWikiApi.AddPage("Container Loop", "");
        fakeWikiApi.AddTemplate("{{contained in|Item Loop}}", "[[Container Loop]]");
        fakeWikiApi.AddTemplate("{{contained in|Container Loop}}", "[[Item Loop]]");

        var graph = await GetSut().GetAcquisitionGraph(["Item Loop"], null, TestContext.Current.CancellationToken);

        var itemLoop = graph.GetNode(NodeType.Item, "Item Loop");
        var containerLoop = graph.GetNode(NodeType.Container, "Container Loop");

        Assert.NotNull(itemLoop);
        Assert.NotNull(containerLoop);

        // Only count nodes with Type = Item and Name = "Item Loop"
        Assert.Single(graph.Nodes.Values, n => n.Id.Type == NodeType.Item && n.Id.Name == "Item Loop");

        // Optional: verify the container node exists too
        Assert.Contains(graph.Nodes.Values, n => n.Id.Type == NodeType.Container && n.Id.Name == "Container Loop");

        // No edges should form infinite loop
        Assert.DoesNotContain(graph.Edges, e => e.Type == EdgeType.SoldBy);
    }

    [Fact]
    public async Task ShouldReturnNothingWhenNoVendorOrAchievementBecauseNoLinks()
    {
        fakeWikiApi.AddPage("Lost Item", "");
        var graph = await GetSut().GetAcquisitionGraph(["Lost Item"], null, TestContext.Current.CancellationToken);

        Assert.Empty(graph.Edges);
    }
}