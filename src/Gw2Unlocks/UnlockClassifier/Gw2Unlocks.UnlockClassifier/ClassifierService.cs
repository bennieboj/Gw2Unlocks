using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
            var oldConfig = await classifierCache.GetClassifierConfigFromCacheAsync(stoppingToken);
            var newConfig = await classifier.ClassifyUnlocks(stoppingToken, null);

            await PrintDiffAsync(oldConfig, newConfig);

            Console.Write("Press y to continue");
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Y)
                await classifierCache.SaveClassifierConfigToCacheAsync(newConfig, CancellationToken.None);
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



    public static async Task PrintDiffAsync(
    ClassifyConfig oldConfig,
    ClassifyConfig newConfig)
    {
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
                Console.WriteLine(groupName);
                PrintAdds(newGroup.Unlocks, 1);
                PrintAllCategories(newGroup, isAdd: true);
                continue;
            }

            if (oldGroup is not null && newGroup is null)
            {
                Console.WriteLine(groupName);
                PrintRemoves(oldGroup.Unlocks, 1);
                PrintAllCategories(oldGroup, isAdd: false);
                continue;
            }

            if (oldGroup is null || newGroup is null)
                continue;

            var groupChanged =
                HasUnlockChanges(oldGroup.Unlocks, newGroup.Unlocks) ||
                HasCategoryChanges(oldGroup, newGroup);

            if (!groupChanged)
                continue;

            Console.WriteLine(groupName);

            PrintUnlockDiff(oldGroup.Unlocks, newGroup.Unlocks, 1);

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
                    WriteIndented(1, categoryName);
                    PrintAdds(newCategory.Unlocks, 2);
                    continue;
                }

                if (oldCategory is not null && newCategory is null)
                {
                    WriteIndented(1, categoryName);
                    PrintRemoves(oldCategory.Unlocks, 2);
                    continue;
                }

                if (oldCategory is null || newCategory is null)
                    continue;

                if (!HasUnlockChanges(oldCategory.Unlocks, newCategory.Unlocks))
                    continue;

                WriteIndented(1, categoryName);
                PrintUnlockDiff(oldCategory.Unlocks, newCategory.Unlocks, 2);
            }
        }
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
        Collection<Unlock> oldUnlocks,
        Collection<Unlock> newUnlocks,
        int indent)
    {
        var oldNames = oldUnlocks.Select(x => x.Name).ToHashSet();
        var newNames = newUnlocks.Select(x => x.Name).ToHashSet();

        var added = newNames.Except(oldNames).OrderBy(x => x);
        var removed = oldNames.Except(newNames).OrderBy(x => x);

        foreach (var name in added)
            WriteColoredIndented(indent, $"+ {name}", ConsoleColor.Green);

        foreach (var name in removed)
            WriteColoredIndented(indent, $"- {name}", ConsoleColor.Red);
    }

    private static void PrintAdds(Collection<Unlock> unlocks, int indent)
    {
        foreach (var unlock in unlocks.OrderBy(x => x.Name))
            WriteColoredIndented(indent, $"+ {unlock.Name}", ConsoleColor.Green);
    }

    private static void PrintRemoves(Collection<Unlock> unlocks, int indent)
    {
        foreach (var unlock in unlocks.OrderBy(x => x.Name))
            WriteColoredIndented(indent, $"- {unlock.Name}", ConsoleColor.Red);
    }

    private static void PrintAllCategories(UnlockGroup group, bool isAdd)
    {
        foreach (var category in group.UnlockCategories.OrderBy(x => x.Name))
        {
            WriteIndented(1, category.Name);

            if (isAdd)
                PrintAdds(category.Unlocks, 2);
            else
                PrintRemoves(category.Unlocks, 2);
        }
    }

    private static void WriteIndented(int indent, string text)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}{text}");
    }

    private static void WriteColoredIndented(
        int indent,
        string text,
        ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;

        Console.WriteLine($"{new string(' ', indent * 2)}{text}");

        Console.ForegroundColor = previous;
    }
}