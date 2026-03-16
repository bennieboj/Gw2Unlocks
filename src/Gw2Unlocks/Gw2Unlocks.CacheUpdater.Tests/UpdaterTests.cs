using Gw2Unlocks.Cache.Contract;
using Gw2Unlocks.Cache.Testing;
using Gw2Unlocks.Gw2SDK.Testing;
using Gw2Unlocks.Testing.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Gw2Unlocks.CacheUpdater.Tests;

public class UpdaterTests : ServiceProviderBasedTest<IUpdater>
{
    private readonly IGw2Cache inMemoryCache;

    public UpdaterTests()
    {
        inMemoryCache = GetService<IGw2Cache>();
    }

    protected override void Configure(IServiceCollection services)
    {
        services.AddFakeGw2Client()
                .AddGw2Caching();

        services.AddUpdater()
                .AddInMemoryGw2Cache();
    }

    [Fact]
    public async Task CanFetchAndCacheItems()
    {
        var sut = GetSut();

        await sut.UpdateItems(TestContext.Current.CancellationToken);

        var json41 = await inMemoryCache.GetCachedAsync("/v2/items", 41);
        var json42 = await inMemoryCache.GetCachedAsync("/v2/items", 42);
        var json43 = await inMemoryCache.GetCachedAsync("/v2/items", 43);
        Assert.NotNull(json41);
        Assert.Contains("Item 41", json41, StringComparison.InvariantCulture);
        Assert.NotNull(json42);
        Assert.Contains("Item 42", json42, StringComparison.InvariantCulture);
        Assert.NotNull(json43);
        Assert.Contains("Item 43", json43, StringComparison.InvariantCulture);
    }

    [Fact]
    public async Task CanFetchAndCacheAllEndpoints()
    {
        var sut = GetSut();

        await sut.UpdateItems(TestContext.Current.CancellationToken);

        var endpoints = new[]
        {
        ("items", 41, "Item 41"),
        ("achievements", 41, "Achievement 41"),
        ("minis", 41, "Mini 41"),
        ("novelties", 41, "Novelty 41"),
        ("titles", 41, "Title 41"),
    };

        foreach (var (endpoint, id, expected) in endpoints)
        {
            var json = await inMemoryCache.GetCachedAsync($"/v2/{endpoint}", id);

            Assert.NotNull(json);
            Assert.Contains(expected, json!, StringComparison.InvariantCulture);
        }
    }
}