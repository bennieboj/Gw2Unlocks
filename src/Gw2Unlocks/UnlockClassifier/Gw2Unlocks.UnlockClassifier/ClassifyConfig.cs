using GuildWars2.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Serialization;

namespace Gw2Unlocks.UnlockClassifier;

public record ClassifyConfig
{
    public Collection<UnlockGroup> UnlockGroups { get; init; } = [];
}
public record UnlockGroup()
{
    public string Name { get; init; } = "";
    [JsonIgnore]
    public Collection<UnlockCriteria> UnlockCriteria { get; init; } = [];

    public Collection<UnlockCategory> UnlockCategories { get; init; } = [];
    public Collection<Unlock> Unlocks { get; init; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(CultureInfo.InvariantCulture, $"{Name}: {Unlocks.Count}");

        foreach (var category in UnlockCategories.Where(c => c.Unlocks.Count > 0))
        {
            sb.Append(CultureInfo.InvariantCulture, $", {category.Name}: {category.Unlocks.Count}");
        }

        return sb.ToString();
    }
}
public record UnlockCategory()
{
    public string Name { get; init; } = "";
    [JsonIgnore]
    public Collection<UnlockCriteria> UnlockCriteria { get; init; } = [];
    public Collection<Unlock> Unlocks { get; init; } = [];

    public override string ToString()
    {
        return $"{Name}: {Unlocks.Count}";
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
}

public enum Type
{
    None,
    Miniature,
    Novelty,
    Skin,
    Achievement
}