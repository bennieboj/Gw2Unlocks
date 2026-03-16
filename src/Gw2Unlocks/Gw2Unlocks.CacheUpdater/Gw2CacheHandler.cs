using Gw2Unlocks.Cache.Contract;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal sealed class Gw2CacheHandler : DelegatingHandler
{
    private readonly IGw2Cache _cache;
    private readonly ILogger<Gw2CacheHandler> _logger;

    public Gw2CacheHandler(IGw2Cache cache, ILogger<Gw2CacheHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var query = request.RequestUri?.Query ?? string.Empty;

        if (!path.StartsWith("/v2/", StringComparison.Ordinal) || !query.Contains("ids=", StringComparison.Ordinal))
            return await base.SendAsync(request, cancellationToken);

        var endpoint = path; // e.g. "/v2/items"
        var response = await base.SendAsync(request, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (!element.TryGetProperty("id", out var idProp)) continue;
                var id = idProp.GetInt32();
                await _cache.AddOrUpdateAsync(endpoint, id, element.GetRawText());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to parse JSON from {Method} {RequestUri}. Response JSON: {Json}",
                request.Method,
                request.RequestUri,
                json.Length > 2000 ? json[..2000] + "..." : json // limit logging size
            );

            throw; // optionally rethrow so callers can handle it
        }

        // Replace content so downstream consumers can read it again
        response.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return response;
    }
}