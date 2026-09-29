using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier.Implementation;

internal sealed class ClassifierService(
    ILogger<ClassifierService> logger,
    IClassifier classifier,
    IClassifierCache classifierCache,
    IHostEnvironment env,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    /// <summary>
    /// Set by GitHub Actions on every runner.
    /// </summary>
    private const string CiEnvironmentVariable = "GITHUB_ACTIONS";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var isCi = Environment.GetEnvironmentVariable(CiEnvironmentVariable) == "true";

        try
        {
            var oldConfig = await classifierCache.GetClassifierConfigFromCacheAsync(stoppingToken);
            var newConfig = await classifier.ClassifyUnlocks(stoppingToken);

            // The diff is the whole product of this step. It goes to the log for a person reading
            // the run, and the pull request workflow reuses the same line to decide how loudly to
            // ask for review. Classifying never stops on account of what the diff says: a removal
            // is a question for a human, not a failure of the classifier, and the pull request is
            // where that question gets asked.
            var diff = ClassifierDiff.Build(oldConfig, newConfig);
            logger.LogInformation("{Diff}", diff.ToMarkdown());

            if (diff.HasRegressions)
            {
                logger.LogWarning(
                    "Classification moved {Moved} unlock(s) and removed {Removed}. " +
                    "This does not fail the run: the change is committed and a human decides on the pull request.",
                    diff.Moved,
                    diff.Removed);
            }

            if (!ShouldSave())
                return;

            await classifierCache.SaveClassifierConfigToCacheAsync(
                newConfig,
                CancellationToken.None);
            logger.LogInformation("Updated classifier cache");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Canceled in ClassifierService");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ClassifierService");

            // A crash used to leave the process exiting 0, so a broken run still reported success
            // and the pipeline carried on with a classification that was never finished.
            if (isCi)
                Environment.ExitCode = 1;
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// Asks before overwriting the cached classification when running on a developer machine, so a
    /// surprising diff can be read first. Everywhere else there is nobody to ask.
    /// </summary>
    private bool ShouldSave()
    {
        if (!env.IsDevelopment())
            return true;

        Console.Write("Press y to continue");
        var input = Console.ReadLine();

        return input is { Length: 1 } && (input[0] == 'y' || input[0] == 'Y');
    }
}
