using GuildWars2;
using GuildWars2.Collections;
using Gw2Unlocks.Cache.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal class Updater(Gw2Client client, IGw2Cache cache) : IUpdater
{
    private readonly Gw2Client _client = client;
    private readonly IGw2Cache _cache = cache;

    // Endpoints we support
    private static readonly string[] Endpoints =
    [
        "items",
        "achievements",
        "minis",
        "novelties",
        "titles",
    ];

    // Non-bulk endpoints have a max 200 ids per request
    private const int NonBulkMaxChunkSize = 200;

    public async Task UpdateApiData(CancellationToken cancellationToken = default)
    {
        foreach (var endpoint in Endpoints)
        {
            await UpdateEndpoint(endpoint, cancellationToken);
        }
    }

    private async Task UpdateEndpoint(string endpoint, CancellationToken cancellationToken)
    {
        var index = await GetIndex(endpoint, cancellationToken);

        // Get only new keys
        var newIds = await _cache.GetNewKeysAsync($"/v2/{endpoint}", index);
        if (!newIds.Any())
            return;

        var progress = new Progress<BulkProgress>(p =>
        {
            Console.WriteLine($"{endpoint}: {p.ResultCount}/{p.ResultTotal}");
        });

        await foreach (var _ in GetBulk(endpoint, newIds, progress, cancellationToken))
        {
            // Gw2CacheHandler caches automatically
        }
    }

    private async Task<IEnumerable<int>> GetIndex(string endpoint, CancellationToken ct)
    {
        return endpoint switch
        {
            "items" => await _client.Items.GetItemsIndex(cancellationToken: ct).ValueOnly(),
            "achievements" => await _client.Hero.Achievements.GetAchievementsIndex(cancellationToken: ct).ValueOnly(),
            "minis" => await _client.Hero.Equipment.Miniatures.GetMiniaturesIndex(cancellationToken: ct).ValueOnly(),
            "novelties" => await _client.Hero.Equipment.Novelties.GetNoveltiesIndex(cancellationToken: ct).ValueOnly(),
            "titles" => await _client.Hero.Achievements.GetTitlesIndex(cancellationToken: ct).ValueOnly(),
            _ => throw new NotSupportedException(endpoint)
        };
    }

    private IAsyncEnumerable<(object, MessageContext)> GetBulk(
        string endpoint,
        IEnumerable<int> ids,
        IProgress<BulkProgress>? progress,
        CancellationToken ct)
    {
        return endpoint switch
        {
            "items" => CastByIdsBulk(_client.Items.GetItemsBulk(ids, progress: progress, cancellationToken: ct)),
            "achievements" => CastByIdsBulk(_client.Hero.Achievements.GetAchievementsBulk(ids, progress: progress, cancellationToken: ct)),
            "minis" => CastByIdsChunked(ids, idsChunk => _client.Hero.Equipment.Miniatures.GetMiniaturesByIds(idsChunk, cancellationToken: ct)),
            "novelties" => CastByIdsChunked(ids, idsChunk => _client.Hero.Equipment.Novelties.GetNoveltiesByIds(idsChunk, cancellationToken: ct)),
            "titles" => CastByIdsChunked(ids, idsChunk => _client.Hero.Achievements.GetTitlesByIds(idsChunk, cancellationToken: ct)),
            _ => throw new NotSupportedException(endpoint)
        };
    }

    /// <summary>
    /// Cast bulk IAsyncEnumerable<(T Value, MessageContext Context)> to IAsyncEnumerable<(object, MessageContext)>
    /// </summary>
    private static async IAsyncEnumerable<(object, MessageContext)> CastByIdsBulk<T>(
        IAsyncEnumerable<(T Value, MessageContext Context)> source)
    {
        await foreach (var (value, ctx) in source)
            yield return (value!, ctx);
    }

    /// <summary>
    /// Non-bulk endpoints with Task<(IImmutableValueSet<T>, MessageContext)> require chunking.
    /// </summary>
    private static async IAsyncEnumerable<(object, MessageContext)> CastByIdsChunked<T>(
        IEnumerable<int> ids,
        Func<IEnumerable<int>, Task<(IImmutableValueSet<T> Value, MessageContext Context)>> getByIds)
    {
        foreach (var chunk in ids.Chunk(NonBulkMaxChunkSize))
        {
            var (set, ctx) = await getByIds(chunk).ConfigureAwait(false);

            foreach (var item in set)
                yield return (item!, ctx);
        }
    }
}