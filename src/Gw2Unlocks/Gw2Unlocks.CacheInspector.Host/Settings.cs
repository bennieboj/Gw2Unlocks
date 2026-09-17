using System;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Gw2Unlocks.CacheInspector.Host;

internal enum DataSet
{
    Achievements, AchievementCategories, Items, Skins, Miniatures, Novelties, Titles,
    Wiki, Graph, Zones
}

internal sealed class LookupSettings : CommandSettings
{
    [CommandArgument(0, "<dataset>")]
    [Description("Achievements, AchievementCategories, Items, Skins, Miniatures, Novelties, Titles, Wiki, Graph, Zones.")]
    public DataSet DataSet { get; set; }

    [CommandArgument(1, "<query>")]
    [Description("Exact API ID or name; wiki title, graph node name, or zone name.")]
    public string Query { get; set; } = "";

    [CommandOption("--search")]
    [Description("Search names/titles by case-insensitive substring instead of exact lookup.")]
    public bool Search { get; set; }

    [CommandOption("--details")]
    [Description("Print full matched records (raw XML for wiki pages).")]
    public bool Details { get; set; }

    public override ValidationResult Validate() =>
        !Enum.IsDefined(DataSet) || string.IsNullOrWhiteSpace(Query)
            ? ValidationResult.Error("Provide a valid dataset and a nonempty query.")
            : ValidationResult.Success();
}

internal sealed class GraphSettings : CommandSettings
{
    [CommandArgument(0, "<page>")]
    public string Page { get; set; } = "";

    [CommandOption("--incoming")]
    [Description("Follow incoming rather than outgoing edges.")]
    public bool Incoming { get; set; }

    [CommandOption("--depth <levels>")]
    [Description("Maximum edge depth; omit for all reachable links.")]
    public int Depth { get; set; } = int.MaxValue;

    public override ValidationResult Validate() =>
        string.IsNullOrWhiteSpace(Page) || Depth < 0
            ? ValidationResult.Error("Provide a page and a nonnegative depth.")
            : ValidationResult.Success();
}
