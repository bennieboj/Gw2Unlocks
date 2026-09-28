using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Gw2Unlocks.UnlockClassifier;

public record ClassifyConfig
{
    /// <summary>Top-level nodes. Order is significant: it drives the sidebar, sitemap and page order.</summary>
    public Collection<UnlockCategory> Categories { get; init; } = [];
}

/// <summary>
/// A node in the classification tree. Nodes nest through <see cref="SubCategories"/>, so a node
/// may hold unlocks directly, hold subcategories, or both.
/// </summary>
public record UnlockCategory()
{
    public string Name { get; init; } = "";
    [JsonIgnore]
    public Collection<UnlockCriteria> UnlockCriteria { get; init; } = [];

    /// <summary>Nested nodes. Order is significant: it drives the sidebar and page order.</summary>
    public Collection<UnlockCategory> SubCategories { get; init; } = [];
    public Collection<Unlock> Unlocks { get; init; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(CultureInfo.InvariantCulture, $"{Name}: {Unlocks.Count}");

        foreach (var subCategory in SubCategories.Where(c => c.Unlocks.Count > 0))
        {
            sb.Append(CultureInfo.InvariantCulture, $", {subCategory.Name}: {subCategory.Unlocks.Count}");
        }

        return sb.ToString();
    }
}

public class Unlock(string name, WikiProcessing.Node node)
{
    public string Name { get; set; } = name;
    public WikiProcessing.Node Node { get; set; } = node;

    public ApiData? ApiData { get; set; }

    public override string ToString()
    {
        return Name;
    }
}

public class ApiData
{
    public int Id { get; set; }
    public Type Type { get; set; } = Type.None;
    public int ChatCodeId { get; set; }
    public string Name { get; set; } = "";
    public Uri IconUrl { get; set; } = new Uri("about:blank");
    public int? IconSheet { get; set; }
    public int? IconX { get; set; }
    public int? IconY { get; set; }

    public string Requirement { get; set; } = "";
    public Uri? RewardIconUrl { get; set; }
    public string? RewardName { get; set; } = "";

    /// <summary>
    /// For a repeatable achievement, the count at which it has awarded its full achievement point
    /// cap, or null when the tiers award fewer points than the cap (so the cap is never reached)
    /// or the achievement is not capped. Null for every other unlock type.
    /// </summary>
    /// <remarks>
    /// The account API only reports an achievement as done once every tier is filled, but a tier
    /// past the cap awards no points, so a player holding every obtainable point still sees the
    /// achievement as incomplete. Comparing progress against this count lets the site treat the
    /// unlock as earned.
    /// </remarks>
    public int? AchievementPointCapReachedAt { get; set; }
}

public enum Type
{
    None,
    Miniature,
    Novelty,
    Skin,
    Achievement
}