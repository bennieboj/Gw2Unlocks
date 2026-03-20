using GuildWars2;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

public class Classifier(IGw2ApiSource apiSource, ILogger<Classifier> logger) : IClassifier
{
    public async Task ClassifyUnlocks(CancellationToken cancellationToken = default)
    {
        var items = await apiSource.GetItemsAsync(cancellationToken);
        var achievements = await apiSource.GetAchievementsAsync(cancellationToken);
        var titles = await apiSource.GetTitlesAsync(cancellationToken);
        var novelties = await apiSource.GetNoveltiesAsync(cancellationToken);
        var miniatures = await apiSource.GetMiniaturesAsync(cancellationToken);
        logger.LogInformation("Loaded from API cache: " +
            "{itemsCount} items, " +
            "{achievementsCount} achievements, " +
            "{titlesCount} titles, " + 
            "{noveltiesCount} novelties, " + 
            "{miniaturesCount} miniatures.",
            items.Count,
            achievements.Count,
            titles.Count,
            novelties.Count,
            miniatures.Count);
    }
}
