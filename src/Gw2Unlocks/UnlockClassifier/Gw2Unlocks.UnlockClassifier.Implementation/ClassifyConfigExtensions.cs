using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Gw2Unlocks.UnlockClassifier.Implementation;

public static class ClassifyConfigExtensions
{
    internal sealed record Categorization
    {
        public CategoryPath Path { get; init; }
    }

    internal sealed record UnlockCriteriaContext<T>(T Criteria, Categorization Categorization) where T : UnlockCriteria
    {
    }
    internal sealed record UnlockContext(Unlock Unlock, Categorization Categorization)
    {
    }



    /// <summary>
    /// Walks the tree depth-first, yielding every node together with the path that reaches it.
    /// </summary>
    private static IEnumerable<(UnlockCategory Node, CategoryPath Path)> Walk(
        IEnumerable<UnlockCategory> categories,
        ImmutableArray<UnlockCategory> prefix)
    {
        foreach (var category in categories)
        {
            var path = new CategoryPath(prefix.Add(category));
            yield return (category, path);

            foreach (var descendant in Walk(category.SubCategories, path.Nodes))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>Every node in the tree, paired with its path from the root.</summary>
    internal static IEnumerable<(UnlockCategory Node, CategoryPath Path)> GetNodes(this ClassifyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Walk(config.Categories, []);
    }

    internal static IEnumerable<UnlockContext> GetUnlocks(this ClassifyConfig config)
    {
        return config.GetNodes()
            .SelectMany(x => x.Node.Unlocks
                .Select(u => new UnlockContext(u, new Categorization { Path = x.Path })));
    }

    internal static IEnumerable<T> GetUnlockCriteria<T>(this ClassifyConfig config) where T : class
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.GetNodes().SelectMany(x => x.Node.UnlockCriteria).OfType<T>();
    }

    internal static IEnumerable<UnlockCriteriaContext<T>> GetUnlockCriteriaWithContext<T>(this ClassifyConfig config)
    where T : UnlockCriteria
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.GetNodes()
            .SelectMany(x => x.Node.UnlockCriteria
                .OfType<T>()
                .Select(c => new UnlockCriteriaContext<T>(
                    c,
                    new Categorization { Path = x.Path })));
    }

    /// <summary>
    /// The criteria in effect for a node: its own, plus those of every ancestor. A node inherits
    /// the whole chain, so a criterion on a root also applies to all of its descendants.
    /// </summary>
    internal static IEnumerable<UnlockCriteria> GetInheritedCriteria(this CategoryPath path)
    {
        return path.Nodes.SelectMany(n => n.UnlockCriteria);
    }
}
