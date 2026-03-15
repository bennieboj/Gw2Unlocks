using Gw2Unlocks.Cache.Contract;
using Gw2Unlocks.Cache.Testing;
using Gw2Unlocks.Gw2SDK.Testing;
using Gw2Unlocks.Testing.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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
        services.AddInMemoryGw2Cache();

        services.AddFakeGw2SDK<FakeGw2Handler>();

        services.AddUpdater();
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
}

#pragma warning disable CA1812 // used in generics above
internal sealed class FakeGw2Handler : HttpMessageHandler
#pragma warning restore CA1812 // used in generics above
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var path = request.RequestUri!.AbsolutePath;
        var query = request.RequestUri.Query;

        if (path.EndsWith("/v2/items", StringComparison.InvariantCulture) && query.Contains("ids=", StringComparison.InvariantCulture))
        {
            // Parse query parameters properly
            var queryDict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query);
            if (!queryDict.TryGetValue("ids", out var idsParam))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return Task.FromResult(response);
            }

            var ids = idsParam.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            // Generate realistic JSON for each item
            var itemsJson = "[" + string.Join(",", ids.Select(id => $@"{{
            ""id"": {id},
            ""name"": ""Item {id}"",
            ""type"": ""Weapon"",
            ""level"": 80,
            ""rarity"": ""Legendary"",
            ""vendor_value"": 100000,
            ""default_skin"": 4678,
            ""game_types"": [""Activity"",""Wvw"",""Dungeon"",""Pve""],
            ""flags"": [""HideSuffix"",""NoSalvage"",""NoSell"",""AccountBindOnUse"",""DeleteWarning""],
            ""restrictions"": [],
            ""chat_link"": ""[&AgHhdwAA]"",
            ""icon"": ""https://render.guildwars2.com/file/A30DA1A1EF05BD080C95AE2EF0067BADCDD0D89D/456014.png"",
            ""details"": {{
                ""type"": ""Greatsword"",
                ""damage_type"": ""Physical"",
                ""min_power"": 1045,
                ""max_power"": 1155,
                ""defense"": 0,
                ""infusion_slots"": [
                    {{ ""flags"": [""Infusion""] }},
                    {{ ""flags"": [""Infusion""] }}
                ],
                ""attribute_adjustment"": 717.024,
                ""suffix_item_id"": 24599,
                ""stat_choices"": [161,155,159,157,158,160],
                ""secondary_suffix_item_id"": null
            }}
        }}")) + "]";

            response.Content = new StringContent(itemsJson);
        }
        else if (path.EndsWith("/v2/items", StringComparison.InvariantCulture))
        {
            response.Content = new StringContent("[41,42,43]");
        }
        else
        {
            response.StatusCode = HttpStatusCode.NotFound;
        }

        response.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return Task.FromResult(response);
    }
}