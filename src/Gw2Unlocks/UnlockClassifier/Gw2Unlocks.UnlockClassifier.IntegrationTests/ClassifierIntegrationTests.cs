using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Testing.Common;
using Gw2Unlocks.WikiGraph.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.UnlockClassifier.IntegrationTests
{
    public class ClassifierIntegrationTests(ITestOutputHelper output) : ServiceProviderBasedTest<IClassifier>(output, LogLevel.Information)
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddCacheDir()
                    .AddJsonCacheApiSource()
                    .AddJsonCacheWikiGraphSource()
                    .AddClassifier();
        }

        [Fact]
        public async Task ClassifyUnlocks()
        {
            await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken);
            await Task.CompletedTask;
        }

        /// Mini Exalted Sage, sold by Exalted Mastery Vendor
        /// Exalted Mastery Vendor is present in both Verdant Brink and Auric Basin
        /// Bus since the Mini Exalted Sage is sold for Lump of Aurilium, which is only acquired in Auric Basin
        /// the unlock should be classified as Auric Basin.
        [Fact]
        public async Task GivenUnlockSoldBySameVendorInMultipleZonesWhenClassifyingUnlockThenShouldReturnZoneLinkedToSellingCurrency()
        {
            var zone = await GetSut().ClassifyUnlock("Mini Exalted Sage", TestContext.Current.CancellationToken);
            Assert.Equal("Auric Basin", zone);
        }
    }
}
