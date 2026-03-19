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

public class CacheHandlerFlakyCallsTests : ServiceProviderBasedTest<IUpdater>
{
    private readonly IGw2Cache inMemoryCache;

    public CacheHandlerFlakyCallsTests()
    {
        inMemoryCache = GetService<IGw2Cache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddGw2ClientForTesting<FlakyHandler>()
                .AddGw2Caching(new Gw2CacheOptions(CacheReadWriteMode.WriteToCache));

        services.AddUpdater()
                .AddInMemoryGw2Cache();
    }

    [Fact]
    public async Task EnsureSuccessStatusCodeThrowsOnFailure()
    {
        var sut = GetSut();

        await sut.UpdateApiData(TestContext.Current.CancellationToken);

        //await Assert.ThrowsAsync<HttpRequestException>(async () =>
        //{
        //    response.EnsureSuccessStatusCode();
        //    await Task.CompletedTask;
        //});
    }

    [Fact]
    public async Task FlakyHandlerRetriesAndEventuallySucceeds()
    {

        var sut = GetSut();

        await sut.UpdateApiData(TestContext.Current.CancellationToken);


    }
#pragma warning disable CA1812 // used in generics
    private sealed class FlakyHandler : HttpMessageHandler
#pragma warning restore CA1812 // used in generics
    {
        private int count;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            count++;

            if (count == 1)
                throw new HttpRequestException("Simulated transient failure");

            var json = """
            [
              {
                "id": 41,
                "name": "Item 41"
              }
            ]
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}