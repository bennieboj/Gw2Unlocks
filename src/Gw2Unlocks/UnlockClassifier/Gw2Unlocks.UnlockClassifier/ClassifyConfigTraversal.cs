using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gw2Unlocks.UnlockClassifier;

/// <summary>
/// Traversal helpers for the classification tree. These live in the contract assembly because
/// they are pure operations over the public model, and consumers such as the website generator
/// need them without depending on the classifier implementation.
/// </summary>
public static class ClassifyConfigTraversal
{
    /// <summary>
    /// Every node in the tree, paired with the names of the categories leading to it, root first.
    /// </summary>
    public static IEnumerable<(UnlockCategory Node, IReadOnlyList<string> Path)> GetPathedCategories(
        this ClassifyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Walk(config.Categories, []);
    }

    /// <summary>Every node in the tree.</summary>
    public static IEnumerable<UnlockCategory> GetAllCategories(this ClassifyConfig config)
    {
        return config.GetPathedCategories().Select(x => x.Node);
    }

    /// <summary>
    /// The unlocks on this node plus, recursively, those of every descendant. The tree stores each
    /// unlock exactly once; rollups are computed on demand at the point of display.
    /// </summary>
    public static Collection<Unlock> GetUnlocksWithDescendants(this UnlockCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        var result = new Collection<Unlock>();
        foreach (var unlock in category.Unlocks)
        {
            result.Add(unlock);
        }

        foreach (var subCategory in category.SubCategories)
        {
            foreach (var unlock in subCategory.GetUnlocksWithDescendants())
            {
                result.Add(unlock);
            }
        }

        return result;
    }

    /// <summary>True when this node or anything beneath it holds at least one unlock.</summary>
    public static bool HasAnyUnlocks(this UnlockCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return category.Unlocks.Count > 0 || category.SubCategories.Any(c => c.HasAnyUnlocks());
    }

    private static IEnumerable<(UnlockCategory Node, IReadOnlyList<string> Path)> Walk(
        IEnumerable<UnlockCategory> categories,
        IReadOnlyList<string> prefix)
    {
        foreach (var category in categories)
        {
            IReadOnlyList<string> path = [.. prefix, category.Name];
            yield return (category, path);

            foreach (var descendant in Walk(category.SubCategories, path))
            {
                yield return descendant;
            }
        }
    }
}
