using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Testing.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.UnlockClassifier.IntegrationTests
{
    public class ClassifierIntegrationTests(ITestOutputHelper output) : ServiceProviderBasedTest<IClassifier>(output)
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddJsonCacheApiSource()
                    .AddClassifier();
        }

        [Fact]
        public async Task Test1Async()
        {
            await GetSut().ClassifyUnlocks(TestContext.Current.CancellationToken);
        }
    }
}
