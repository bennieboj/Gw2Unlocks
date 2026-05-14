using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

internal sealed class ClassifierService(
    ILogger<ClassifierService> logger,
    IClassifier classifier,
    IClassifierCache classifierCache,
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

            Console.Write("Press y to continue");
            var input = Console.ReadLine();

            if (input is { Length: 1 } && (input[0] == 'y' || input[0] == 'Y'))
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

        var oldGroups = oldConfig.UnlockGroups.ToDictionary(x => x.Name);
        var newGroups = newConfig.UnlockGroups.ToDictionary(x => x.Name);

        var groupNames = oldGroups.Keys
            .Union(newGroups.Keys)
            .OrderBy(x => x);

        foreach (var groupName in groupNames)
        {
            oldGroups.TryGetValue(groupName, out var oldGroup);
            newGroups.TryGetValue(groupName, out var newGroup);

            if (oldGroup is null && newGroup is not null)
            {
                logger.LogInformation("{GroupName}", groupName);
                PrintAdds(logger, newGroup.Unlocks, 1);
                PrintAllCategories(logger, newGroup, true);
                continue;
            }

            if (oldGroup is not null && newGroup is null)
            {
                logger.LogInformation("{GroupName}", groupName);
                PrintRemoves(logger, oldGroup.Unlocks, 1, oldLocations, newLocations);
                PrintAllCategoriesRemoved(logger, oldGroup, oldLocations, newLocations);
                continue;
            }

            if (oldGroup is null || newGroup is null)
                continue;

            var groupChanged =
                HasUnlockChanges(oldGroup.Unlocks, newGroup.Unlocks) ||
                HasCategoryChanges(oldGroup, newGroup);

            if (!groupChanged)
                continue;

            logger.LogInformation("{GroupName}", groupName);

            PrintUnlockDiff(
                logger,
                oldGroup.Unlocks,
                newGroup.Unlocks,
                1,
                oldLocations,
                newLocations);

            var oldCategories = oldGroup.UnlockCategories.ToDictionary(x => x.Name);
            var newCategories = newGroup.UnlockCategories.ToDictionary(x => x.Name);

            var categoryNames = oldCategories.Keys
                .Union(newCategories.Keys)
                .OrderBy(x => x);

            foreach (var categoryName in categoryNames)
            {
                oldCategories.TryGetValue(categoryName, out var oldCategory);
                newCategories.TryGetValue(categoryName, out var newCategory);

                if (oldCategory is null && newCategory is not null)
                {
                    WriteIndented(logger, 1, categoryName);
                    PrintAdds(logger, newCategory.Unlocks, 2);
                    continue;
                }

                if (oldCategory is not null && newCategory is null)
                {
                    WriteIndented(logger, 1, categoryName);
                    PrintRemoves(
                        logger,
                        oldCategory.Unlocks,
                        2,
                        oldLocations,
                        newLocations);
                    continue;
                }

                if (oldCategory is null || newCategory is null)
                    continue;

                if (!HasUnlockChanges(oldCategory.Unlocks, newCategory.Unlocks))
                    continue;

                WriteIndented(logger, 1, categoryName);

                PrintUnlockDiff(
                    logger,
                    oldCategory.Unlocks,
                    newCategory.Unlocks,
                    2,
                    oldLocations,
                    newLocations);
            }
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, string> BuildLocationMap(ClassifyConfig config)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var group in config.UnlockGroups)
        {
            foreach (var unlock in group.Unlocks)
                map[unlock.Name] = group.Name;

            foreach (var category in group.UnlockCategories)
            {
                foreach (var unlock in category.Unlocks)
                    map[unlock.Name] = $"{group.Name} > {category.Name}";
            }
        }

        return map;
    }

    private static bool HasCategoryChanges(UnlockGroup oldGroup, UnlockGroup newGroup)
    {
        var oldCategories = oldGroup.UnlockCategories.ToDictionary(x => x.Name);
        var newCategories = newGroup.UnlockCategories.ToDictionary(x => x.Name);

        var names = oldCategories.Keys.Union(newCategories.Keys);

        foreach (var name in names)
        {
            oldCategories.TryGetValue(name, out var oldCategory);
            newCategories.TryGetValue(name, out var newCategory);

            if (oldCategory is null || newCategory is null)
                return true;

            if (HasUnlockChanges(oldCategory.Unlocks, newCategory.Unlocks))
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

        var added = newNames.Except(oldNames).OrderBy(x => x);
        var removed = oldNames.Except(newNames).OrderBy(x => x);

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

    private static void PrintAllCategories(
        ILogger logger,
        UnlockGroup group,
        bool isAdd)
    {
        foreach (var category in group.UnlockCategories.OrderBy(x => x.Name))
        {
            WriteIndented(logger, 1, category.Name);

            if (isAdd)
                PrintAdds(logger, category.Unlocks, 2);
        }
    }

    private static void PrintAllCategoriesRemoved(
        ILogger logger,
        UnlockGroup group,
        Dictionary<string, string> oldLocations,
        Dictionary<string, string> newLocations)
    {
        foreach (var category in group.UnlockCategories.OrderBy(x => x.Name))
        {
            WriteIndented(logger, 1, category.Name);

            PrintRemoves(
                logger,
                category.Unlocks,
                2,
                oldLocations,
                newLocations);
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