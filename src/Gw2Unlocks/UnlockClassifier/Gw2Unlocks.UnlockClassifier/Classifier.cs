using GuildWars2;
using GuildWars2.Items;
using Gw2Unlocks.Cache.Contract;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

public class Classifier(Gw2Client client, ILogger<Classifier> logger) : IClassifier
{
    public async Task ClassifyUnlocks(CancellationToken cancellationToken = default)
    {
        int lastProcessed = 0;
        var progress = new Progress<BulkProgress>(p =>
        {
            lastProcessed = p.ResultCount;
            Console.WriteLine($"Classifier fetched {p.ResultCount}/{p.ResultTotal}");
        });

        var list = new List<Item>();

        try
        {
            await foreach (var (item, _) in client.Items.GetItemsBulk(progress: progress, cancellationToken: cancellationToken))
        {
            list.Add(item);
        }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "err");
            Console.WriteLine($"Bulk failed after {lastProcessed} items");
            throw;
        }


        IEnumerable<Item> items = list;
        items.GetEnumerator();
    }
}
