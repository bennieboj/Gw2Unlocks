using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            var oldConfig = await classifierCache.GetClassifierConfigFromCacheAsync(stoppingToken);
            var newConfig = await classifier.ClassifyUnlocks(stoppingToken);

            await PrintDiffAsync(logger, oldConfig, newConfig);
            var input = "";
            var isDev = env.IsDevelopment();
            if (isDev)
            {
                Console.Write("Press y to continue");
                input = Console.ReadLine();
            }

            if (!isDev || input is { Length: 1 } && (input[0] == 'y' || input[0] == 'Y'))
            {
                await classifierCache.SaveClassifierConfigToCacheAsync(
                    newConfig,
                    CancellationToken.None);
                logger.LogInformation("Updated classifier cache");
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Canceled in ClassifierService");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ClassifierService");
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }

    public static Task PrintDiffAsync(
        ILogger logger,
        ClassifyConfig oldConfig,
        ClassifyConfig newConfig)
    {
        var oldLocations = BuildLocationMap(oldConfig);
        var newLocations = BuildLocationMap(newConfig);

        logger.LogInformation("Listing all diffs:");

        var oldNodes = oldConfig.GetNodes().ToDictionary(x => x.Path.Key, x => x.Node);
        var newNodes = newConfig.GetNodes().ToDictionary(x => x.Path.Key, x => x.Node);

        // Walk the new config in its own order so the diff reads in the same order as the site,
        // then append nodes that only the old config has.
        var keys = newConfig.GetNodes()
            .Select(x => x.Path.Key)
            .Concat(oldConfig.GetNodes()
                .Select(x => x.Path.Key)
                .Where(k => !newNodes.ContainsKey(k)));

        foreach (var key in keys)
        {
            oldNodes.TryGetValue(key, out var oldNode);
            newNodes.TryGetValue(key, out var newNode);

            // Indent by depth so the output mirrors the shape of the tree.
            var depth = key.Split(PathSeparator).Length;
            var label = key.Replace(PathSeparator, '>');

            if (oldNode is null && newNode is not null)
            {
                WriteIndented(logger, depth, label);
                PrintAdds(logger, newNode.Unlocks, depth + 1);
                continue;
            }

            if (oldNode is not null && newNode is null)
            {
                WriteIndented(logger, depth, label);
                PrintRemoves(logger, oldNode.Unlocks, depth + 1, oldLocations, newLocations);
                continue;
            }

            if (oldNode is null || newNode is null || !HasUnlockChanges(oldNode.Unlocks, newNode.Unlocks))
                continue;

            WriteIndented(logger, depth, label);
            PrintUnlockDiff(logger, oldNode.Unlocks, newNode.Unlocks, depth + 1, oldLocations, newLocations);
        }

        return Task.CompletedTask;
    }

    private const char PathSeparator = '';

    private static Dictionary<string, string> BuildLocationMap(ClassifyConfig config)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (node, path) in config.GetNodes())
        {
            foreach (var unlock in node.Unlocks)
                map[unlock.Name] = path.ToString();
        }

        return map;
    }

    /// <summary>
    /// True when the node's own unlocks or anything beneath it changed.
    /// </summary>
    private static bool HasSubtreeChanges(UnlockCategory oldNode, UnlockCategory newNode)
    {
        if (HasUnlockChanges(oldNode.Unlocks, newNode.Unlocks))
            return true;

        var oldChildren = oldNode.SubCategories.ToDictionary(x => x.Name);
        var newChildren = newNode.SubCategories.ToDictionary(x => x.Name);

        foreach (var name in oldChildren.Keys.Union(newChildren.Keys))
        {
            oldChildren.TryGetValue(name, out var oldChild);
            newChildren.TryGetValue(name, out var newChild);

            if (oldChild is null || newChild is null)
                return true;

            if (HasSubtreeChanges(oldChild, newChild))
                return true;
        }

        return false;
    }

    private static bool HasUnlockChanges(
        Collection<Unlock> oldUnlocks,
        Collection<Unlock> newUnlocks)
    {
        var oldNames = oldUnlocks.Select(x => x.Name).ToHashSet();
        var newNames = newUnlocks.Select(x => x.Name).ToHashSet();

        return !oldNames.SetEquals(newNames);
    }

    private static void PrintUnlockDiff(
        ILogger logger,
        Collection<Unlock> oldUnlocks,
        Collection<Unlock> newUnlocks,
        int indent,
        Dictionary<string, string> oldLocations,
        Dictionary<string, string> newLocations)
    {
        var oldNames = oldUnlocks.Select(x => x.Name).ToHashSet();
        var newNames = newUnlocks.Select(x => x.Name).ToHashSet();

        var added = newNames.Except(oldNames).OrderBy(x => x, StringComparer.Ordinal);
        var removed = oldNames.Except(newNames).OrderBy(x => x, StringComparer.Ordinal);

        foreach (var name in added)
        {
            if (oldLocations.TryGetValue(name, out var previousLocation))
            {
                WriteIndented(
                    logger,
                    indent,
                    $"[*] {name} (from {previousLocation})");
            }
            else
            {
                WriteIndented(
                    logger,
                    indent,
                    $"[+] {name}");
            }
        }

        foreach (var name in removed)
        {
            if (!newLocations.ContainsKey(name) &&
                oldLocations.TryGetValue(name, out var previousLocation))
            {
                WriteIndented(
                    logger,
                    indent,
                    $"[-] {name} (from {previousLocation})");
            }
        }
    }


    private static void PrintAdds(
        ILogger logger,
        Collection<Unlock> unlocks,
        int indent)
    {
        foreach (var unlock in unlocks.OrderBy(x => x.Name))
            WriteIndented(logger, indent, $"[+] {unlock.Name}");
    }


    private static void PrintRemoves(
        ILogger logger,
        Collection<Unlock> unlocks,
        int indent,
        Dictionary<string, string> oldLocations,
        Dictionary<string, string> newLocations)
    {
        foreach (var unlock in unlocks.OrderBy(x => x.Name))
        {
            if (!newLocations.ContainsKey(unlock.Name) &&
                oldLocations.TryGetValue(unlock.Name, out var previousLocation))
            {
                WriteIndented(
                    logger,
                    indent,
                    $"[-] {unlock.Name} (from {previousLocation})");
            }
        }
    }

    private static void WriteIndented(
        ILogger logger,
        int indent,
        string text)
    {
        logger.LogInformation("{Text}", $"{new string(' ', indent * 2)}{text}");
    }
}