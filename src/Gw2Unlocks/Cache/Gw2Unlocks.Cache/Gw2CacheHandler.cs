using Gw2Unlocks.Cache.Contract;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Cache;

#pragma warning disable CA1812
internal sealed class Gw2CacheHandler(IGw2Cache cache, ILogger<Gw2CacheHandler> logger, Gw2CacheOptions options) : DelegatingHandler
#pragma warning restore CA1812
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var query = request.RequestUri?.Query ?? string.Empty;

        if (!path.StartsWith("/v2/", StringComparison.Ordinal) || !query.Contains("ids=", StringComparison.Ordinal))
            return await base.SendAsync(request, cancellationToken);

        var endpoint = path;

        // -------------------------
        // READ FROM CACHE MODE
        // -------------------------
        if (options.ReadWriteMode == CacheReadWriteMode.ReadFromCache)
        {
            var queryDict = QueryHelpers.ParseQuery(query);

            if (!queryDict.TryGetValue("ids", out var idsParam))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            var ids = idsParam
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse);

            var results = new List<string>();

            foreach (var id in ids)
            {
                var cached = await cache.GetCachedAsync(endpoint, id);
                if (cached != null)
                    results.Add(cached);
            }

            var json = "[" + string.Join(",", results) + "]";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        // -------------------------
        // NORMAL NETWORK CALL
        // -------------------------
        var response = await base.SendAsync(request, cancellationToken);

        // -------------------------
        // WRITE TO CACHE MODE
        // -------------------------
        if (options.ReadWriteMode == CacheReadWriteMode.WriteToCache)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(json);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (!element.TryGetProperty("id", out var idProp)) continue;

                    var id = idProp.GetInt32();
                    await cache.AddOrUpdateAsync(endpoint, id, element.GetRawText());
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex,
                    "Failed to parse JSON from {Method} {RequestUri}. Response JSON: {Json}",
                    request.Method,
                    request.RequestUri,
                    json.Length > 2000 ? json[..2000] + "..." : json
                );

                throw;
            }

            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return response;
    }
}