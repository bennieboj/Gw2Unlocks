using Gw2Unlocks.UnlockClassifier;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gw2Unlocks.Website;

internal sealed class PageModel
{
    public string Css { get; set; } = "";
    public string Js { get; set; } = "";
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Url { get; set; } = "";

    public Collection<Unlock> Unlocks { get; set; } = [];

    public List<SidebarCategoryModel> Sidebar { get; set; } = [];

    /// <summary>The node this page is for, or null for the "all unlocks" page.</summary>
    public UnlockCategory? Category { get; set; }

    public List<TypeGroupModel> TypeGroups { get; set; } = [];

    public string UnlockMapJson { get; set; } = "NOTJSON";

    /// <summary>
    /// Slash-joined slugs from the root to the current node, e.g. "heart-of-thorns/auric-basin".
    /// Matches the key used in the unlock map and the data-path attributes in the template.
    /// </summary>
    public string CurrentSlug { get; set; } = "all";
}

internal sealed class TypeGroupModel
{
    public Type Type { get; set; } = Type.None;
    public string Label { get; set; } = "";

    public int Total { get; set; }

    // unknown until JS runs
    public int? Unlocked { get; set; }

    public List<UnlockRenderModel> Unlocks { get; set; } = [];
}

internal sealed class UnlockRenderModel
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public string? IconAtlasStyleCss { get; set; }

    public int AtlasSheet { get; set; }

    public int AtlasX { get; set; }

    public int AtlasY { get; set; }
    public string Requirement { get; set; } = "";

    public string? RewardIcon { get; set; } = "";

    public int RewardAtlasSheet { get; set; }

    public int RewardAtlasX { get; set; }

    public int RewardAtlasY { get; set; }
    public string? RewardName { get; set; } = "";

    public string WikiUrl { get; set; } = "";

    public Type Type { get; set; } = Type.None;
}

/// <summary>
/// One entry in the sidebar. Nested categories carry their own children, so the sidebar mirrors
/// the shape of the classification tree at any depth.
/// </summary>
internal sealed class SidebarCategoryModel
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    /// <summary>Slash-joined slugs from the root, used as the key into the unlock map.</summary>
    public string Slug { get; set; } = "";

    public bool IsWip { get; set; }

    /// <summary>Indent level in the sidebar, 0 for a root node.</summary>
    public int Depth { get; set; }
}

internal sealed class UnlockMapModel
{
    /// <summary>
    /// Unlock ids per node, keyed by the node's full slug path from the root, e.g.
    /// "heart-of-thorns/auric-basin". Each entry covers the node and everything beneath it, so a
    /// root entry is the total for that whole branch.
    /// </summary>
    public Dictionary<string, Dictionary<Type, List<int>>> Categories { get; set; } = [];
}