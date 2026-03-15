using GuildWars2;
using Gw2Unlocks.Cache.Contract;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal class Updater(Gw2Client client, IGw2Cache cache) : IUpdater
{
    private readonly Gw2Client _client = client;
    private readonly IGw2Cache _cache = cache;

    public async Task UpdateItems(CancellationToken cancellationToken = default)
    {
        // Step 1: Get all item IDs
        var itemIds = await _client.Items.GetItemsIndex(cancellationToken: cancellationToken).ValueOnly();

        // Step 2: Filter out items we already have
        var newIds = await _cache.GetNewKeysAsync("/v2/items", itemIds);
        newIds = newIds.Take(500);
        if (!newIds.Any())
            return;

        // Step 3: Fetch new items in bulk
        var progress = new Progress<BulkProgress>(p =>
        {
            Console.WriteLine($"Fetched {p.ResultCount}/{p.ResultTotal}");
        });
        await foreach (var (item, context) in _client.Items.GetItemsBulk(newIds, progress: progress, cancellationToken: cancellationToken))
        {
            // Convert to JSON and store
            var json = JsonSerializer.Serialize(item);
            await _cache.AddOrUpdateAsync("/v2/items", item.Id, json);
        }
    }
}

//var x = await _client.Hero.Equipment.Miniatures.GetMiniaturesIndex().ValueOnly();