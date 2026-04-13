using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

internal sealed class ClassifierService(ILogger<BackgroundService> logger, IClassifier classifier, IClassifierCache classifierCache) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(logger);
        try
        {
            var config = await classifier.ClassifyUnlocks(stoppingToken, null);
            await classifierCache.SaveClassifierConfigToCacheAsync(config, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Canceled in ClassifierService");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ClassifierService");
        }
    }
}
