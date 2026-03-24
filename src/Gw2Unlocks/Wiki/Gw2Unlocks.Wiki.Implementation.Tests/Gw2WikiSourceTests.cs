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

        var result = await GetSut().GetAllUnlocks(["Mini Exalted Sage"], TestContext.Current.CancellationToken);
        var root = Assert.Single(result).Root;

        var paths = Gw2WikiSource.GetPathsToTerminal(root);
        var chain = Assert.Single(paths);

        Assert.Collection(chain,
            n => Assert.IsType<ItemAcquisitionNode>(n),
            n => Assert.IsType<VendorAcquisitionNode>(n),
            n => Assert.IsType<AreaAcquisitionNode>(n),
            n => Assert.IsType<ZoneAcquisitionNode>(n)
        );
    }


    [Fact]
    public async Task ContainedInShouldRecurseToVendor()
    {
        fakeWikiApi.AddPage("Item A", "");
        fakeWikiApi.AddTemplate("{{contained in|Item A}}", "[[Container A]]");
        fakeWikiApi.AddTemplate("{{Sold by|Container A}}",
            CreateSoldByTable("Vendor A", "Tarir, the Forgotten City", "Auric Basin"));

        var result = await GetSut().GetAllUnlocks(["Item A"], TestContext.Current.CancellationToken);
        var root = Assert.Single(result).Root;

        var paths = Gw2WikiSource.GetPathsToTerminal(root);
        var chain = Assert.Single(paths);

        Assert.Collection(chain,
            n => Assert.IsType<ItemAcquisitionNode>(n),
            n => Assert.IsType<ContainerAcquisitionNode>(n),
            n => Assert.IsType<VendorAcquisitionNode>(n),
            n => Assert.IsType<AreaAcquisitionNode>(n),
            n => Assert.IsType<ZoneAcquisitionNode>(n)
        );

        Assert.Equal("Item A", chain[0].Title);
        Assert.Equal("Container A", chain[1].Title);
        Assert.Equal("Vendor A", chain[2].Title);
        Assert.Equal("Tarir, the Forgotten City", chain[3].Title);
        Assert.Equal("Auric Basin", chain[4].Title);
    }

    [Fact]
    public async Task SkinShouldRecurseThroughItems()
    {
        fakeWikiApi.AddPage("Skin X (skin)", "");
        fakeWikiApi.AddTemplate("{{skin list|Skin X (skin)}}", "[[Item X]]");
        fakeWikiApi.AddTemplate("{{Sold by|Item X}}",
            CreateSoldByTable("Vendor X", "Tarir, the Forgotten City", "Auric Basin"));

        var result = await GetSut().GetAllUnlocks(["Skin X (skin)"], TestContext.Current.CancellationToken);
        var root = Assert.Single(result).Root;

        var paths = Gw2WikiSource.GetPathsToTerminal(root);
        var chain = Assert.Single(paths);

        Assert.Collection(chain,
            n => Assert.IsType<ItemAcquisitionNode>(n),   // Skin root
            n => Assert.IsType<ItemAcquisitionNode>(n),   // Item X
            n => Assert.IsType<VendorAcquisitionNode>(n),
            n => Assert.IsType<AreaAcquisitionNode>(n),
            n => Assert.IsType<ZoneAcquisitionNode>(n)
        );
    }

    [Fact]
    public async Task MultiplePathsShouldAggregateResults()
    {
        fakeWikiApi.AddPage("Item Multi", "");
        fakeWikiApi.AddTemplate("{{contained in|Item Multi}}", "[[Container A]] [[Container B]]");
        fakeWikiApi.AddTemplate("{{Sold by|Container A}}",
            CreateSoldByTable("Vendor A", "Tarir, the Forgotten City", "Auric Basin"));
        fakeWikiApi.AddTemplate("{{Sold by|Container B}}",
            CreateSoldByTable("Vendor B", "Tarir, the Forgotten City", "Auric Basin"));

        var result = await GetSut().GetAllUnlocks(["Item Multi"], TestContext.Current.CancellationToken);
        var root = Assert.Single(result).Root;

        var paths = Gw2WikiSource.GetPathsToTerminal(root);

        Assert.Equal(2, paths.Count);

        Assert.Contains(paths, p => p.Any(n => n.Title == "Vendor A"));
        Assert.Contains(paths, p => p.Any(n => n.Title == "Vendor B"));
    }


    [Fact]
    public async Task AchievementShouldStopTraversal()
    {
        fakeWikiApi.AddPage("Skin A", "{{achievement box|Achievement A}}");

        var result = await GetSut().GetAllUnlocks(["Skin A"], TestContext.Current.CancellationToken);
        var root = Assert.Single(result).Root;

        Assert.IsType<ItemAcquisitionNode>(root);

        var next = Assert.Single(root.Next);

        Assert.IsType<AchievementAcquisitionNode>(next);
        Assert.Equal("Achievement A", next.Title);
    }

    [Fact]
    public async Task ShouldAvoidInfiniteLoops()
    {
        fakeWikiApi.AddPage("Item Loop", "");
        fakeWikiApi.AddPage("Container Loop", "");
        fakeWikiApi.AddTemplate("{{contained in|Item Loop}}", "[[Container Loop]]");
        fakeWikiApi.AddTemplate("{{contained in|Container Loop}}", "[[Item Loop]]");

        var result = await GetSut().GetAllUnlocks(["Item Loop"] ,TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ShouldReturnNothingWhenNoVendorOrAchievementBecauseNoLinks()
    {
        fakeWikiApi.AddPage("Lost Item", "");
        var result = await GetSut().GetAllUnlocks(["Lost Item"], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ShouldReturnNothingWhenNoVendorOrAchievementBecauseNoResultsForSoldBy()
    {
        fakeWikiApi.AddPage("Not Sold Item", "");
        fakeWikiApi.AddTemplate("{{achievement box|No results for sold by}}", "[[Achievement NeverFound]]");
        fakeWikiApi.AddTemplate("{{Sold by|Not Sold Item}}", "No results for sold by");
        var result = await GetSut().GetAllUnlocks(["Not Sold Item"], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ShouldReturnNothingWhenNoVendorOrAchievementBecauseNoResultsForAchievementBox()
    {
        fakeWikiApi.AddPage("Unachieved Item", "");
        fakeWikiApi.AddTemplate("{{Sold by|Category:Pages with empty semantic mediawiki query results}}",
            CreateSoldByTable("Vendor A", "Tarir, the Forgotten City", "Auric Basin"));
        fakeWikiApi.AddTemplate("{{achievement box|Unachieved Item}}", "[[Category:Pages with empty semantic mediawiki query results]]");

        var result = await GetSut().GetAllUnlocks(["Unachieved Item"], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ShouldReturnNothingWhenNoVendorOrAchievementBecauseNoResultsForContainedIn()
    {
        fakeWikiApi.AddPage("NotContained Item", "");
        fakeWikiApi.AddTemplate("{{Sold by|Category:Pages with empty semantic mediawiki query results}}",
            CreateSoldByTable("Vendor A", "Tarir, the Forgotten City", "Auric Basin"));
        fakeWikiApi.AddTemplate("{{contained in|NotContained Item}}", "[[Category:Pages with empty semantic mediawiki query results]]");

        var result = await GetSut().GetAllUnlocks(["NotContained Item"], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SoldByWithMultipleAreasShouldFollowAllPaths()
    {
        // Template returns two areas for the vendor
        fakeWikiApi.AddTemplate("{{Sold by|Mini Exalted Sage}}",
            @"[[SMW::off]]
{| class=""npc sortable table""
! Vendor
! Area
! Zone
! Cost <nowiki/>
! Notes
|-
| [[File:Exalted Mastery Vendor.png|20x20px|link=Exalted Mastery Vendor#vendor32|Exalted Mastery Vendor]] [[Exalted Mastery Vendor#vendor32|Exalted Mastery Vendor]]
| [[Noble Ledges|Noble Ledges]]<br>[[Tarir, the Forgotten City|Tarir, the Forgotten City]]
| [[:Verdant Brink|Verdant Brink]]<br>[[:Auric Basin|Auric Basin]]
| style=""text-align:right"" | 1,000&nbsp;[[File:Lump of Aurillium.png|20px|link=Lump of Aurillium|Lump of Aurillium]]&nbsp;+&nbsp;<span class=""price"" style=""white-space:nowrap;"" data-sort-value=""50000"">5&nbsp;[[File:Gold coin.png|link=Coin|18px|Gold coin]]</span><nowiki/>
| Requires the [[mastery]] [[Exalted Acceptance|Exalted Acceptance]].
|-
|}[[SMW::on]]");

        var result = await GetSut().GetAllUnlocks(["Mini Exalted Sage"], TestContext.Current.CancellationToken);

        var unlock = Assert.Single(result, u => u.Name == "Mini Exalted Sage");
        var root = unlock.Root;

        // Collect all paths to terminals (zone/achievement)
        var paths = Gw2WikiSource.GetPathsToTerminal(root);

        // We expect 2 paths: one via Tarir → Auric Basin, one via Noble Ledges → Verdant Brink
        Assert.Equal(2, paths.Count);

        Assert.Contains(paths, path =>
         path.Count == 4 &&
         path[0].Title == "Mini Exalted Sage" &&
         path[1].Title == "Exalted Mastery Vendor" &&
         path[1] is VendorAcquisitionNode &&
         path[2].Title == "Tarir, the Forgotten City" &&
         path[3].Title == "Auric Basin"
     );

        Assert.Contains(paths, path =>
            path.Count == 4 &&
            path[0].Title == "Mini Exalted Sage" &&
            path[1].Title == "Exalted Mastery Vendor" &&
            path[1] is VendorAcquisitionNode &&
            path[2].Title == "Noble Ledges" &&
            path[3].Title == "Verdant Brink"
        );
    }
}