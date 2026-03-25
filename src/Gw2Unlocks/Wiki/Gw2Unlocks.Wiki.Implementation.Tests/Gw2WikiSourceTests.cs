using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki.WikiApi;
using Gw2Unlocks.Wiki.WikiApi.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
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
        fakeWikiApi.AddTemplate("{{Sold by|Mini Exalted Sage}}",
            CreateSoldByTable("Exalted Mastery Vendor", "Tarir, the Forgotten City", "Auric Basin"));

        var graph = await GetSut().GetAcquisitionGraph(["Mini Exalted Sage"], null, TestContext.Current.CancellationToken);

        var item = graph.Nodes.Single(n => n.Id.Type == NodeType.Item && n.Id.Name == "Mini Exalted Sage");
        var vendor = graph.Nodes.Single(n => n.Id.Type == NodeType.Vendor && n.Id.Name == "Exalted Mastery Vendor");
        var area = graph.Nodes.Single(n => n.Id.Type == NodeType.Area && n.Id.Name == "Tarir, the Forgotten City");
        var zone = graph.Nodes.Single(n => n.Id.Type == NodeType.Zone && n.Id.Name == "Auric Basin");

        Assert.Contains(graph.Edges, e => e.From == item.Id && e.To == vendor.Id && e.Type == EdgeType.SoldBy);
        Assert.Contains(graph.Edges, e => e.From == vendor.Id && e.To == area.Id && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area.Id && e.To == zone.Id && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task ContainedInShouldRecurseToVendor()
    {
        fakeWikiApi.AddPage("Item A", "");
        fakeWikiApi.AddTemplate("{{contained in|Item A}}", "[[Container A]]");
        fakeWikiApi.AddTemplate("{{Sold by|Container A}}",
            CreateSoldByTable("Vendor A", "Tarir, the Forgotten City", "Auric Basin"));

        var graph = await GetSut().GetAcquisitionGraph(["Item A"], null, TestContext.Current.CancellationToken);

        var item = graph.Nodes.Single(n => n.Id.Type == NodeType.Item && n.Id.Name == "Item A");
        var container = graph.Nodes.Single(n => n.Id.Type == NodeType.Container && n.Id.Name == "Container A");
        var vendor = graph.Nodes.Single(n => n.Id.Type == NodeType.Vendor && n.Id.Name == "Vendor A");
        var area = graph.Nodes.Single(n => n.Id.Type == NodeType.Area && n.Id.Name == "Tarir, the Forgotten City");
        var zone = graph.Nodes.Single(n => n.Id.Type == NodeType.Zone && n.Id.Name == "Auric Basin");

        Assert.Contains(graph.Edges, e => e.From == item.Id && e.To == container.Id && e.Type == EdgeType.Contains);
        Assert.Contains(graph.Edges, e => e.From == container.Id && e.To == vendor.Id && e.Type == EdgeType.SoldBy);
        Assert.Contains(graph.Edges, e => e.From == vendor.Id && e.To == area.Id && e.Type == EdgeType.LocatedIn);
        Assert.Contains(graph.Edges, e => e.From == area.Id && e.To == zone.Id && e.Type == EdgeType.LocatedIn);
    }

    [Fact]
    public async Task SkinShouldRecurseThroughItems()
    {
        fakeWikiApi.AddPage("Skin X (skin)", "");
        fakeWikiApi.AddTemplate("{{skin list|Skin X (skin)}}", "[[Item X]]");
        fakeWikiApi.AddTemplate("{{Sold by|Item X}}",
            CreateSoldByTable("Vendor X", "Tarir, the Forgotten City", "Auric Basin"));

        var graph = await GetSut().GetAcquisitionGraph(["Skin X (skin)"], null, TestContext.Current.CancellationToken);

        var skin = graph.Nodes.Single(n => n.Id.Type == NodeType.Item && n.Id.Name == "Skin X (skin)");
        var item = graph.Nodes.Single(n => n.Id.Type == NodeType.Item && n.Id.Name == "Item X");
        var vendor = graph.Nodes.Single(n => n.Id.Type == NodeType.Vendor && n.Id.Name == "Vendor X");
        var area = graph.Nodes.Single(n => n.Id.Type == NodeType.Area && n.Id.Name == "Tarir, the Forgotten City");
        var zone = graph.Nodes.Single(n => n.Id.Type == NodeType.Zone && n.Id.Name == "Auric Basin");

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

        var item = graph.Nodes.Single(n => n.Id.Type == NodeType.Item && n.Id.Name == "Skin A");
        var achievement = graph.Nodes.Single(n => n.Id.Type == NodeType.Achievement && n.Id.Name == "Achievement A");

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

        // Only count nodes with Type = Item and Name = "Item Loop"
        Assert.Single(graph.Nodes, n => n.Id.Type == NodeType.Item && n.Id.Name == "Item Loop");
        // Optional: verify the container node exists too
        Assert.Contains(graph.Nodes, n => n.Id.Type == NodeType.Container && n.Id.Name == "Item Loop");
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