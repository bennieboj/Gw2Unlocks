using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gw2Unlocks.UnlockClassifier;

public static class ClassifyConfigExtensions
{
    internal sealed class UnlockCriteriaContext<T> where T : UnlockCriteria
    {
        public required T Criteria { get; init; }
        public UnlockGroup? Group { get; init; }
        public UnlockCategory? Category { get; init; }
        public string? GroupOfCategoryName { get; init; }
    }

    internal static IEnumerable<T> GetUnlockCriteria<T>(this ClassifyConfig config) where T : UnlockCriteria
    {
        ArgumentNullException.ThrowIfNull(config);
        var categoryCriteria = config.UnlockGroups.SelectMany(g => g.UnlockCategories).SelectMany(c => c.UnlockCriteria).OfType<T>();
        var groupCriteria = config.UnlockGroups.SelectMany(c => c.UnlockCriteria).OfType<T>();
        return categoryCriteria.Union(groupCriteria);
    }

    internal static IEnumerable<UnlockCriteriaContext<T>> GetUnlockCriteriaWithContext<T>(this ClassifyConfig config)
    where T : UnlockCriteria
    {
        ArgumentNullException.ThrowIfNull(config);

        // Group-level criteria
        var groupCriteria = config.UnlockGroups
            .SelectMany(g => g.UnlockCriteria
                .OfType<T>()
                .Select(c => new UnlockCriteriaContext<T>
                {
                    Criteria = c,
                    Group = g,
                    Category = null,
                    GroupOfCategoryName = null,
                }));

        // Category-level criteria
        var categoryCriteria = config.UnlockGroups
            .SelectMany(g => g.UnlockCategories
                .SelectMany(cat => cat.UnlockCriteria
                    .OfType<T>()
                    .Select(c => new UnlockCriteriaContext<T>
                    {
                        Criteria = c,
                        Group = null,
                        Category = cat,
                        GroupOfCategoryName = g.Name,
                    })));

        return groupCriteria.Concat(categoryCriteria);
    }
}

public record ClassifyConfig(Collection<UnlockGroup> UnlockGroups) {
}
public record UnlockGroup(string Name, Collection<UnlockCriteria> UnlockCriteria, Collection<UnlockCategory> UnlockCategories)
{
    public Collection<Unlock> Unlocks { get; init; } = new();
}
public record UnlockCategory(string Name, Collection<UnlockCriteria> UnlockCriteria)
{
    public Collection<Unlock> Unlocks { get; init; } = new();
}

public class Unlock(string name, WikiProcessing.Node node)
{
    public string Name { get; set; } = name;
    public WikiProcessing.Node Node { get; set; } = node;
    public object? ApiData { get; set; }
}