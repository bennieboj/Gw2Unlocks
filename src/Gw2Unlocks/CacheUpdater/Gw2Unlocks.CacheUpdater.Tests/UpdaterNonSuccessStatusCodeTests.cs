using Gw2Unlocks.Cache;
using Gw2Unlocks.Cache.Contract;
using Gw2Unlocks.Cache.Testing;
using Gw2Unlocks.Api.Testing;
using Gw2Unlocks.Testing.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.CacheUpdater.Tests;

public class UpdaterNonSuccessStatusCodeTests : ServiceProviderBasedTest<IUpdater>
{
    private readonly IGw2Cache inMemoryCache;

    public UpdaterNonSuccessStatusCodeTests()
    {
        inMemoryCache = GetService<IGw2Cache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddGw2ClientForTesting<ErrorHandler>()
                .AddGw2Caching(new Gw2CacheOptions(CacheReadWriteMode.WriteToCache));

        services.AddUpdater()
                .AddInMemoryGw2Cache();
    }

    [Fact]
    public async Task EnsureSuccessStatusCodeThrowsOnFailure()
    {
        var sut = GetSut();

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        //var handler = new ErrorHandler();
        //var client = new HttpClient(handler);

        //var request = new HttpRequestMessage(HttpMethod.Get, "https://api.guildwars2.com/v2/items?ids=41");

        //var response = await client.SendAsync(request);

        //await Assert.ThrowsAsync<HttpRequestException>(async () =>
        //{
        //    response.EnsureSuccessStatusCode();
        //    await Task.CompletedTask;
        //});
    }
#pragma warning disable CA1812 // used in generics
    private sealed class ErrorHandler : HttpMessageHandler
#pragma warning restore CA1812 // used in generics
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}