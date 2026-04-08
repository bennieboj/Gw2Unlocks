using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.Wiki;
using Gw2Unlocks.WikiProcessing.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.WikiProcessing.IntegrationTests;

public class GetZoneDataTests : ServiceProviderBasedTest<IGw2WikiGraphSource>
{
    private readonly Gw2WikiIntegrationTestSuccessResponseFake fakeWikiApi;

    public GetZoneDataTests(ITestOutputHelper outputHelper) : base(outputHelper)
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
    public async Task GetZoneDataShouldLoad()
    {
        fakeWikiApi.FileName = "zone.xml";
        var zoneData = await GetSut().GetZoneData(TestContext.Current.CancellationToken);

        var southsunCove = zoneData.Zones.Single(z => z.Name == "Southsun Cove");
        Assert.Empty(southsunCove.AchievementCategories);
        
        var dryTop = zoneData.Zones.Single(z => z.Name == "Dry Top");
        Assert.Single(dryTop.AchievementCategories);
        Assert.Contains("Dry Top", dryTop.AchievementCategories);

        var emberBay = zoneData.Zones.Single(z => z.Name == "Ember Bay");
        Assert.Single(emberBay.AchievementCategories);
        Assert.Contains("Rising Flames", emberBay.AchievementCategories);

        var innerNayos = zoneData.Zones.Single(z => z.Name == "Inner Nayos");
        Assert.Equal(3, innerNayos.AchievementCategories.Count);
        Assert.Contains("Inner Nayos: Heitor's Territory", innerNayos.AchievementCategories);
        Assert.Contains("Inner Nayos: Nyedra Surrounds", innerNayos.AchievementCategories);
        Assert.Contains("Inner Nayos: Citadel of Zakiros", innerNayos.AchievementCategories);
    }
}