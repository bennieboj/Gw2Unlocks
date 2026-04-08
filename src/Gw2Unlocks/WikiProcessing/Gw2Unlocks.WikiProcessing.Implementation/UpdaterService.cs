using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.WikiProcessing.Implementation;

internal sealed class UpdaterService(ILogger<BackgroundService> logger, IGw2WikiGraphSource graphSource, IGw2WikiGraphCache graphCache) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(logger);
        try
        {
            var graph = await graphSource.GetAcquisitionGraph(stoppingToken);
            await graphCache.SaveAcquisitionGraphToCacheAsync(graph, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Canceled in UpdaterService");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdaterService");
        }
    }
}
