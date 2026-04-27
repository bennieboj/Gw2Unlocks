using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.CacheUpdater;

internal sealed class UpdaterService(ILogger<UpdaterService> logger, IUpdater updater, IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(logger);
        try
        {
            await updater.UpdateApiData(stoppingToken);
            await updater.UpdateWikiData(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Canceled in UpdaterService");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdaterService");
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }
}
