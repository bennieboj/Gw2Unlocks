using Gw2Unlocks.Api;
using Gw2Unlocks.Cache;
using Gw2Unlocks.Cache.Contract;
using Gw2Unlocks.Cache.SqlLite;
using Gw2Unlocks.Testing.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.UnlockClassifier.IntegrationTests
{
    public class ClassifierIntegrationTests : ServiceProviderBasedTest<IClassifier>
    {
        public ClassifierIntegrationTests()
        {
        }

        protected override void Configure(IServiceCollection services)
        {
            services.AddGw2Client()
                    .AddGw2Caching(new Gw2CacheOptions(CacheReadWriteMode.ReadFromCache));

            services.AddUpdater()
                    .AddSqlLiteGw2Cache("db.sqlite");
        }

        [Fact]
        public async Task Test1Async()
        {
            await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken);
        }
    }
}
