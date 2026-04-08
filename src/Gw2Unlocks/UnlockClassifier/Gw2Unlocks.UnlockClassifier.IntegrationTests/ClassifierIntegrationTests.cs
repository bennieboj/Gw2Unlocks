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

        /// Mini Exalted Sage, sold by Exalted Mastery Vendor
        /// Exalted Mastery Vendor is present in both Verdant Brink and Auric Basin
        /// Bus since the Mini Exalted Sage is sold for Lump of Aurilium, which is only acquired in Auric Basin
        /// the unlock should be classified as Auric Basin.
        [Fact]
        public async Task GivenUnlockSoldBySameVendorInMultipleZonesWhenClassifyingUnlockThenShouldReturnZoneLinkedToSellingCurrency()
        {
            var results = await GetSut().ClassifyUnlocks("Mini Exalted Sage", TestContext.Current.CancellationToken);
            var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
            var category = group.UnlockCategories.Single(c => c.Name == "Auric Basin");
            var unlock = category.Unlocks.Single(c => c.Name == "Mini Exalted Sage");

            Assert.NotNull(unlock);
            Assert.NotNull(unlock.ApiData);
        }
        [Fact]
        public async Task GivenUnlockSkinSHouldContainApiData()
        {
            var results = await GetSut().ClassifyUnlocks("Bladed Helmet (skin)", TestContext.Current.CancellationToken);
            var group = results.UnlockGroups.Single(g => g.Name == "Heart of Thorns");
            var category = group.UnlockCategories.Single(c => c.Name == "Verdant Brink");
            var unlock = category.Unlocks.Single(c => c.Name == "Bladed Helmet (skin)");

            Assert.NotNull(unlock);
            Assert.NotNull(unlock.ApiData);
        }
    }
}
