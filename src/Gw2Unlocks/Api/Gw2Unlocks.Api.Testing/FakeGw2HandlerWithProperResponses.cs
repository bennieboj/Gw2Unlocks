using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Testing;

#pragma warning disable CA1812 // used in generics
public sealed class FakeGw2HandlerWithProperResponses : HttpMessageHandler
#pragma warning restore CA1812 // used in generics
{
    private static readonly string[] Endpoints =
    [
        "items",
        "achievements",
        "minis",
        "novelties",
        "titles",
    ];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var path = request.RequestUri!.AbsolutePath;
        var query = request.RequestUri.Query;

        var endpoint = Endpoints.FirstOrDefault(e => path.Equals($"/v2/{e}", StringComparison.OrdinalIgnoreCase));
        if (endpoint is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return Task.FromResult(response);
        }

        if (query.Contains("ids=", StringComparison.Ordinal))
        {
            var queryDict = QueryHelpers.ParseQuery(query);
            if (!queryDict.TryGetValue("ids", out var idsParam))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return Task.FromResult(response);
            }

            var ids = idsParam.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            var json = "[" + string.Join(",", ids.Select(id => GetFakeEntityJson(endpoint, int.Parse(id, CultureInfo.InvariantCulture)))) + "]";
            response.Content = new StringContent(json);
        }
        else
        {
            // index endpoint returns a list of ids
            response.Content = new StringContent("[41,42,43]");
        }

        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return Task.FromResult(response);
    }

    private static string GetFakeEntityJson(string endpoint, int id) => endpoint switch
    {
        "items" => $@"{{
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
                ""secondary_suffix_item_id"": """"
            }}
        }}",

        "achievements" => $@"{{
            ""id"": {id},
            ""name"": ""Achievement {id}"",
            ""description"": ""Fake achievement description"",
            ""requirement"": ""Do something"",
            ""locked_text"": ""Do something, but hidden"",
            ""type"": ""Default"",
            ""flags"": [],
            ""bits"": [],
            ""tiers"": [],
            ""rewards"": []
        }}",

        "minis" => $@"{{
            ""id"": {id},
            ""name"": ""Mini {id}"",
            ""icon"": ""https://render.guildwars2.com/file/FAKEMINI{id}/mini.png"",
            ""order"": 1,
            ""item_id"": {id + 1000}
        }}",

        "novelties" => $@"{{
            ""id"": {id},
            ""name"": ""Novelty {id}"",
            ""description"": ""Fake novelty description"",
            ""icon"": ""https://render.guildwars2.com/file/FAKENOVELTY{id}/novelty.png"",
            ""slot"": ""HeldItem"",
            ""unlock_item"": [{id + 2000}]
        }}",

        "titles" => $@"{{
            ""id"": {id},
            ""name"": ""Title {id}"",
            ""achievement"": {id + 3000},
            ""achievements"": [{id + 3000}]
        }}",

        _ => "{}"
    };
}
