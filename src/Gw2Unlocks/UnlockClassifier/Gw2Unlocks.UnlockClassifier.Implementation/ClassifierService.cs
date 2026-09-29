using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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

    /// <summary>
    /// When set, the rendered diff is also written here. The classifier is the only stage that
    /// knows what changed, so it is the only one that can put the diff in the pull request without
    /// somebody scraping it back out of the run log.
    /// </summary>
    private const string DiffReportVariable = "CLASSIFIER_DIFF_REPORT";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var isCi = Environment.GetEnvironmentVariable(CiEnvironmentVariable) == "true";

        try
        {
            var oldConfig = await classifierCache.GetClassifierConfigFromCacheAsync(stoppingToken);
            var newConfig = await classifier.ClassifyUnlocks(stoppingToken);

            // The diff is the whole product of this step. It goes to the log for a person reading
            // the run, to a file the workflow turns into the pull request body, and nowhere else.
            // Classifying never stops on account of what the diff says: a removal is a question for
            // a human, not a failure of the classifier, and the pull request is where it gets asked.
            var diff = ClassifierDiff.Build(oldConfig, newConfig);
            var markdown = diff.ToMarkdown();

            logger.LogInformation("{Diff}", markdown);
            WriteDiffReport(markdown);

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

    /// <summary>
    /// Writes the diff where the workflow can pick it up. Best effort: the log already carries the
    /// diff, so a failure here costs the pull request its body but must not fail the run.
    /// </summary>
    private void WriteDiffReport(string markdown)
    {
        var path = Environment.GetEnvironmentVariable(DiffReportVariable);

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            File.WriteAllText(path, markdown);
            logger.LogInformation("Wrote the classification diff to {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not write the classification diff to {Path}", path);
        }
    }
}
