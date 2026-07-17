using Gw2Unlocks.UnlockClassifier;
using System.Collections.Generic;

namespace Gw2Unlocks.Website;

internal sealed class PageModel
{
    public string Css { get; set; } = "";
    public string Js { get; set; } = "";
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Url { get; set; } = "";

    public List<Unlock> Unlocks { get; set; } = [];

    public List<SidebarGroupModel> Sidebar { get; set; } = [];

    public UnlockGroup? Group { get; set; }

    public UnlockCategory? Category { get; set; }

    public List<TypeGroupModel> TypeGroups { get; set; } = [];

    public string UnlockMapJson { get; set; } = "NOTJSON";

    public string CurrentSlug =>
        Category != null
            ? SlugHelper.Slugify(Category.Name)
            : Group != null
                ? SlugHelper.Slugify(Group.Name)
                : "all";
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

internal sealed class SidebarGroupModel
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public string Slug { get; set; } = "";
    
    public bool IsWip { get; set; }

    public List<SidebarCategoryModel> Categories { get; set; } = [];
}

internal sealed class SidebarCategoryModel
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public string Slug { get; set; } = "";

    public bool IsWip { get; set; }
}

internal sealed class UnlockMapModel
{
    public Dictionary<string, Dictionary<Type, List<int>>> Groups { get; set; } = [];
    public Dictionary<string, Dictionary<Type, List<int>>> Categories { get; set; } = [];
}