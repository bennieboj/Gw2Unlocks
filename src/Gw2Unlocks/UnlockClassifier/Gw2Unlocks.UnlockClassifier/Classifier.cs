using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Rewards;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using Gw2Unlocks.WikiProcessing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Gw2Unlocks.UnlockClassifier.ClassifyConfigExtensions;

namespace Gw2Unlocks.UnlockClassifier;

public class Classifier(IGw2ApiSource apiSource, IGw2WikiProcessingSource wikigraphSource, ILogger<Classifier> logger) : IClassifier
{
    private AcquisitionGraph? graph;
    private Collection<Miniature>? miniatures;
    private Collection<Item>? items;
    private Collection<EquipmentSkin>? skins;
    private Collection<Achievement>? achievements;
    private Collection<Title>? titles;
    private Collection<Novelty>? novelties;

    private static readonly ClassifyConfig classifyConfig = new(
        [
            new UnlockGroup("Heart of Thorns", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Verdant Brink", [ new ZoneCriteria("Verdant Brink"), new CurrencyCriteria("Airship Part") ]),
                new UnlockCategory("Auric Basin", [ new ZoneCriteria("Auric Basin"), new CurrencyCriteria("Lump of Aurillium") ]),
                new UnlockCategory("Tangled Depths", [ new ZoneCriteria("Tangled Depths") ]),
                new UnlockCategory("Dragon's Stand", [ new ZoneCriteria("Dragon's Stand") ]),
            ]),

            new UnlockGroup("Path of Fire", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Crystal Oasis", [ new ZoneCriteria("Crystal Oasis") ]),
                new UnlockCategory("Desert Highlands", [ new ZoneCriteria("Desert Highlands") ]),
                new UnlockCategory("Elon Riverlands", [ new ZoneCriteria("Elon Riverlands") ]),
                new UnlockCategory("The Desolation", [ new ZoneCriteria("The Desolation") ]),
                new UnlockCategory("Domain of Vabbi", [ new ZoneCriteria("Domain of Vabbi") ]),
            ]),

            new UnlockGroup("End of Dragons", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Seitung Province", [ new ZoneCriteria("Seitung Province") ]),
                new UnlockCategory("New Kaineng City", [ new ZoneCriteria("New Kaineng City") ]),
                new UnlockCategory("The Echovald Wilds", [ new ZoneCriteria("The Echovald Wilds") ]),
                new UnlockCategory("Arborstone", [ new ZoneCriteria("Arborstone") ]),
                new UnlockCategory("Dragon's End", [ new ZoneCriteria("Dragon's End") ]),
                new UnlockCategory("Gyala Delve", [ new ZoneCriteria("Gyala Delve") ]),
            ]),

            new UnlockGroup("Secrets of the Obscure", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Skywatch Archipelago", [ new ZoneCriteria("Skywatch Archipelago") ]),
                new UnlockCategory("The Wizard's Tower", [ new ZoneCriteria("The Wizard's Tower") ]),
                new UnlockCategory("Amnytas", [ new ZoneCriteria("Amnytas") ]),
                new UnlockCategory("Inner Nayos", [ new ZoneCriteria("Inner Nayos") ]),
            ]),

            new UnlockGroup("Janthir Wilds", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Lowland Shore", [ new ZoneCriteria("Lowland Shore") ]),
                new UnlockCategory("Janthir Syntri", [ new ZoneCriteria("Janthir Syntri") ]),
            ]),
            new UnlockGroup("LW Season 1", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Season 1", [ new NoOpCriteria() ]),
            ]),

            new UnlockGroup("LW Season 2", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Dry Top", [ new ZoneCriteria("Dry Top") ]),
                new UnlockCategory("The Silverwastes", [ new ZoneCriteria("The Silverwastes") ]),
            ]),

            new UnlockGroup("LW Season 3", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Bloodstone Fen", [ new ZoneCriteria("Bloodstone Fen") ]),
                new UnlockCategory("Ember Bay", [ new ZoneCriteria("Ember Bay") ]),
                new UnlockCategory("Bitterfrost Frontier", [ new ZoneCriteria("Bitterfrost Frontier") ]),
                new UnlockCategory("Lake Doric", [ new ZoneCriteria("Lake Doric") ]),
                new UnlockCategory("Draconis Mons", [ new ZoneCriteria("Draconis Mons") ]),
                new UnlockCategory("Siren's Landing", [ new ZoneCriteria("Siren's Landing") ]),
            ]),

            new UnlockGroup("LW Season 4", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Domain of Istan", [ new ZoneCriteria("Domain of Istan") ]),
                new UnlockCategory("Sandswept Isles", [ new ZoneCriteria("Sandswept Isles") ]),
                new UnlockCategory("Domain of Kourna", [ new ZoneCriteria("Domain of Kourna") ]),
                new UnlockCategory("Jahai Bluffs", [ new ZoneCriteria("Jahai Bluffs") ]),
                new UnlockCategory("Thunderhead Peaks", [ new ZoneCriteria("Thunderhead Peaks") ]),
                new UnlockCategory("Dragonfall", [ new ZoneCriteria("Dragonfall") ]),
            ]),

            new UnlockGroup("Icebrood Saga", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Grothmar Valley", [ new ZoneCriteria("Grothmar Valley") ]),
                new UnlockCategory("Bjora Marches", [ new ZoneCriteria("Bjora Marches") ]),
                new UnlockCategory("Drizzlewood Coast", [ new ZoneCriteria("Drizzlewood Coast") ]),
            ]),

            new UnlockGroup("Dungeons", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Ascalonian Catacombs", [ new NoOpCriteria() ]),
                new UnlockCategory("Caudecus's Manor", [ new NoOpCriteria() ]),
                new UnlockCategory("Twilight Arbor", [ new NoOpCriteria() ]),
                new UnlockCategory("Sorrow's Embrace", [ new NoOpCriteria() ]),
                new UnlockCategory("Citadel of Flame", [ new NoOpCriteria() ]),
                new UnlockCategory("Honor of the Waves", [ new NoOpCriteria() ]),
                new UnlockCategory("Crucible of Eternity", [ new NoOpCriteria() ]),
                new UnlockCategory("The Ruined City of Arah", [ new NoOpCriteria() ]),
            ]),

            new UnlockGroup("Raid Encounters", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Old Lion's Court", [ new NoOpCriteria() ]),
                new UnlockCategory("Shiverpeaks Pass", [ new NoOpCriteria() ]),
                new UnlockCategory("Voice of the Fallen and Claw of the Fallen", [ new NoOpCriteria() ]),
                new UnlockCategory("Fraenir of Jormag", [ new NoOpCriteria() ]),
                new UnlockCategory("Boneskinner", [ new NoOpCriteria() ]),
                new UnlockCategory("Whisper of Jormag", [ new NoOpCriteria() ]),
                new UnlockCategory("Forging Steel", [ new NoOpCriteria() ]),
                new UnlockCategory("Cold War", [ new NoOpCriteria() ]),
                new UnlockCategory("Aetherblade Hideout", [ new NoOpCriteria() ]),
                new UnlockCategory("Xunlai Jade Junkyard", [ new NoOpCriteria() ]),
                new UnlockCategory("Kaineng Overlook", [ new NoOpCriteria() ]),
                new UnlockCategory("Harvest Temple", [ new NoOpCriteria() ]),
                new UnlockCategory("Cosmic Observatory", [ new NoOpCriteria() ]),
                new UnlockCategory("Temple of Febe", [ new NoOpCriteria() ]),
                new UnlockCategory("Guardian's Glade", [ new NoOpCriteria() ]),
            ]),

            new UnlockGroup("Raids", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Spirit Vale", [ new NoOpCriteria() ]),
                new UnlockCategory("Salvation Pass", [ new NoOpCriteria() ]),
                new UnlockCategory("Stronghold of the Faithful", [ new NoOpCriteria() ]),
                new UnlockCategory("Bastion of the Penitent", [ new NoOpCriteria() ]),
                new UnlockCategory("Hall of Chains", [ new NoOpCriteria() ]),
                new UnlockCategory("Mythwright Gambit", [ new NoOpCriteria() ]),
                new UnlockCategory("The Key of Ahdashim", [ new NoOpCriteria() ]),
            ]),

            new UnlockGroup("PvP / WvW", [ new NoOpCriteria() ],
            [
                new UnlockCategory("PvP", [ new NoOpCriteria() ]),
                new UnlockCategory("WvW", [ new NoOpCriteria() ]),
            ]),

            new UnlockGroup("Festivals", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Lunar New Year", [ new NoOpCriteria() ]),
                new UnlockCategory("Super Adventure Box", [ new NoOpCriteria() ]),
                new UnlockCategory("Dragon Bash", [ new NoOpCriteria() ]),
                new UnlockCategory("Festival of the Four Winds", [ new NoOpCriteria() ]),
                new UnlockCategory("Halloween", [ new NoOpCriteria() ]),
                new UnlockCategory("Wintersday", [ new TokenCriteria("Snow Diamond") ]),
            ]),

            new UnlockGroup("Cities", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Divinity's Reach", [ new NoOpCriteria() ]),
                new UnlockCategory("The Grove", [ new NoOpCriteria() ]),
                new UnlockCategory("Hoelbrak", [ new NoOpCriteria() ]),
                new UnlockCategory("Rata Sum", [ new NoOpCriteria() ]),
                new UnlockCategory("Black Citadel", [ new NoOpCriteria() ]),
                new UnlockCategory("Lion's Arch", [ new NoOpCriteria() ]),
                new UnlockCategory("Eye of the North", [ new NoOpCriteria() ]),
            ]),

            new UnlockGroup("Other", [ new NoOpCriteria() ],
            [
                new UnlockCategory("Elite Specializations", [ new NoOpCriteria() ]),
                new UnlockCategory("Guild", [ new NoOpCriteria() ]),
                new UnlockCategory("Mystic Forge", [ new NoOpCriteria() ]),
                new UnlockCategory("Crafting", [ new NoOpCriteria() ]),
                new UnlockCategory("Black Lion Claim Ticket", [ new NoOpCriteria() ]),
                new UnlockCategory("Black Lion Statuette", [ new NoOpCriteria() ]),
                new UnlockCategory("Gathering Tools", [ new NoOpCriteria() ]),
                new UnlockCategory("Gem Store", [ new NoOpCriteria() ]),
                new UnlockCategory("General", [ new NoOpCriteria() ]),
                new UnlockCategory("Fractals", [ new NoOpCriteria() ]),
                new UnlockCategory("Wizard's Vault", [ new NoOpCriteria() ]),
            ]),
        ]
    );
    private static readonly IEnumerable<UnlockCriteriaContext<TokenCriteria>> tokenCriteria = classifyConfig.GetUnlockCriteriaWithContext<TokenCriteria>();

    public async Task<ClassifyConfig> ClassifyUnlocks(CancellationToken cancellationToken, string? unlockToLookup = null)
    {
        Stopwatch sw = Stopwatch.StartNew();
        await Init(cancellationToken);
        logger.LogInformation("Initialization completed in {elapsed} ms", sw.ElapsedMilliseconds);

        var nodes = graph!.Nodes.ToList();
        if(unlockToLookup != null)
        {
            nodes = [.. nodes.Where(n => n.Key.Equals(unlockToLookup, StringComparison.OrdinalIgnoreCase))];
        }

        int i = 0;
        foreach (var (key, node) in nodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Cancellation requested, returning partial result.");
                break;
            }

            if (!ShouldClassify(node))
            {
                logger.LogDebug("Skipping {key} ({type})", key, node.Type);
                continue;
            }
            logger.LogInformation("Finding {key} ({type})", key, node.Type);

            await ClassifyUnlock(key);

            i++;
            logger.LogInformation("{progress}/{total}", i, nodes.Count);
        }

        foreach(var group in classifyConfig.UnlockGroups)
        {

            logger.LogInformation("Group: {groupName} ({unlockCount} unlocks)", group.Name, group.Unlocks.Count);
            foreach (var unlock in group.Unlocks)
            {
                var apidata = GetApiData(unlock.Node, unlock.Name);
                unlock.ApiData = apidata;
                //logger.LogInformation("    Unlock: {unlockName} ({unlockType})", unlock.Name, unlock.Node.Type);
            }
            foreach (var category in group.UnlockCategories)
            {
                logger.LogInformation("  Category: {categoryName} ({unlockCount} unlocks)", category.Name, category.Unlocks.Count);
                foreach (var unlock in category.Unlocks)
                {
                    var apidata = GetApiData(unlock.Node, unlock.Name);
                    unlock.ApiData = apidata;
                    //logger.LogInformation("    Unlock: {unlockName} ({unlockType})", unlock.Name, unlock.Node.Type);
                }
            }
        }

        return classifyConfig;
    }


    private async Task<ClassifyConfig> ClassifyUnlock(string unlock)
    {
        var result = Classify(graph!, unlock);
        if (result != null)
        {
            var zone = result.Value;
            logger.LogInformation("{zone.Key} ({zone.Node.Type})", zone.Key, zone.Node.Type);

            logger.LogInformation("Path:");
            foreach (var step in zone.Path)
            {
                logger.LogInformation("  -> {step}", step);
            }
            //return zone.Key;
        }
        return classifyConfig;
    }

    private async Task Init(CancellationToken cancellationToken)
    {
        graph ??= await wikigraphSource.GetAcquisitionGraph(cancellationToken);
        miniatures ??= await apiSource.GetMiniaturesAsync(cancellationToken);
        items ??= await apiSource.GetItemsAsync(cancellationToken);
        skins ??= await apiSource.GetSkinsAsync(cancellationToken);
        achievements ??= await apiSource.GetAchievementsAsync(cancellationToken);
        titles ??= await apiSource.GetTitlesAsync(cancellationToken);
        novelties ??= await apiSource.GetNoveltiesAsync(cancellationToken);
    }

    private static bool ShouldClassify(Node node) =>
    node switch
    {
        { 
            Type: NodeType.Skin,
            Metadata: var metadata
        } when metadata.TryGetValue("type", out var type) &&
               (
                   !type.Equals("Fishing Rod", StringComparison.OrdinalIgnoreCase) &&
                   !type.Equals("Mining", StringComparison.OrdinalIgnoreCase) &&
                   !type.Equals("Logging", StringComparison.OrdinalIgnoreCase) &&
                   !type.Equals("Foraging", StringComparison.OrdinalIgnoreCase)
               )
            => true,
        {
            Type: NodeType.Item,
            Metadata: var metadata
        } when metadata.TryGetValue("type", out var type) &&
               (
                   type.Equals("miniature", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("Novelty", StringComparison.OrdinalIgnoreCase)
               )
            => true,

        // ❌ Everything else
        _ => false
    };

    private readonly List<string> debugtitles = [
        //"Plush Zhaia Backpack (skin)",
        //"Plush Zhaia Backpack",
        //"Halloween Vendor",
        //"Hooligan's Route",
        //"Lion's Arch",
        "Mini Foostivoo the Merry",
        "Mini Exalted Sage",
        "Exalted Mastery Vendor",
        "Noble Ledges",
        "Verdant Brink",
        "Noble's Folly",
        "Zinn's Stash",
        "Glob of Ectoplasm",
        "Bladed Helmet (skin)",
        "Adam"
        //"Tarir, the Forgotten City",
        //"Auric Basin",
        //"Axe of the Dragon's Deep",
        //"Arah Weapons Box",
        //"Dungeon Weapon Container",
    ];

    private class SearchState
    {
        public string Key { get; init; } = default!;
        public string? Cost { get; init; } // store raw cost string
        public EdgeType? IncomingEdgeType { get; init; }
    }

    private object? GetApiData(Node node, string startKey)
    {
        if(miniatures == null || skins == null || achievements == null || titles == null || novelties == null)
        {
            logger.LogWarning("API data not initialized when trying to get API data for {key} ({type})", startKey, node.Type);
            return null;
        }

        object? result = null;
        if (node.Type == NodeType.Item && node.Metadata.TryGetValue("type", out var metadataTypeMini) && metadataTypeMini == "miniature"
            && node.Metadata.TryGetValue("miniature id", out var miniId) && !string.IsNullOrEmpty(miniId) && int.TryParse(miniId, out var miniIdInt))
        {
            result = miniatures.Single(m => m.Id == miniIdInt);
        }

        if (node.Type == NodeType.Item && node.Metadata.TryGetValue("type", out var metadataTypeNovelty) && metadataTypeNovelty.Contains("novelty", StringComparison.OrdinalIgnoreCase)
            && node.Metadata.TryGetValue("novelty-id", out var noveltyId) && !string.IsNullOrEmpty(noveltyId) && int.TryParse(noveltyId, out var noveltyIdInt))
        {
            result = novelties.Single(m => m.Id == noveltyIdInt);
        }

        if (node.Type == NodeType.Skin && node.Metadata.TryGetValue("id", out var skinId) && !string.IsNullOrEmpty(skinId) && int.TryParse(skinId, out var skinIdInt))
        {
            result = skins.Single(i => i.Id == skinIdInt);
        }

        if (result == null)
        {
            logger.LogWarning("No API data found for {key} ({type})", startKey, node.Type);
        }

        return result;
    }

    private (string Key, Node Node, List<string> Path)? Classify(
        AcquisitionGraph graph,
        string startKey)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<SearchState>();
        var parent = new Dictionary<string, string?>();
        var startNode = graph.GetNode(startKey);

        if(startNode == null)
            return null;

        visited.Add(startKey);
        queue.Enqueue(new SearchState
        {
            Key = startKey,
            Cost = null,
            IncomingEdgeType = null,
        });
        parent[startKey] = null;

        while (queue.Count > 0)
        {
            var searchState = queue.Dequeue();
            var currentKey = searchState.Key;
            var current = graph.GetNode(currentKey);

            if(debugtitles.Contains(currentKey))
            {
                logger.LogInformation("Visiting {key} ({type}) with cost {cost}", currentKey, current?.Type, searchState.Cost);
            }

            if (current == null)
                continue;

            if (searchState.IncomingEdgeType == EdgeType.GatheredFrom 
                && current.Type == NodeType.Gw2Object && current.Metadata.TryGetValue("type", out var objectType) && objectType != "chest")
            {
                continue;
            }

            // When we reach a zone/city → validate cost
            if (current.Type == NodeType.Location &&
                current.Metadata.TryGetValue("type", out var value) &&
                (value.Equals("Zone", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("City", StringComparison.OrdinalIgnoreCase)))
            {
                var zone = currentKey;
                foreach (var group in classifyConfig.UnlockGroups)
                {
                    foreach (var category in group.UnlockCategories)
                    {
                        // Does the category have a ZoneCriteria that matches this zone?
                        var hasZone = category.UnlockCriteria
                            .OfType<ZoneCriteria>()
                            .Any(z => z.Matches(zone));

                        if (!hasZone)
                            continue;


                        // Does the category have a CurrencyCriteria that matches this currency?
                        var validCurrencies = category.UnlockCriteria
                            .OfType<CurrencyCriteria>().ToList();

                        var cost = searchState.Cost;
                        if (cost == null || validCurrencies.Count == 0 || validCurrencies.Any(c => c.Matches(cost)))
                        {

                            var path = CategorizeAndBuildPath(group.Name, category.Name, startKey, startNode, currentKey, parent);
                            return (currentKey, current, path);
                        }
                    }
                }
            }

            if(current.Type == NodeType.Item && current.Metadata.TryGetValue("id", out var itemId) && int.TryParse(itemId, out var itemIdInt)
                && achievements != null)
            {
                var foundAchi = achievements.Where(a => a.Rewards != null && a.Rewards.OfType<ItemReward>().Any(ir => ir.Id == itemIdInt));
                if(foundAchi.Any())
                {
                }

            }

            IEnumerable<Edge> edges = [.. graph.Edges.Where(e => e.From == currentKey)];

            if(edges.Any(
                e => e.Type == EdgeType.HasIngredient && e.Metadata != null
                && 
                    (e.Metadata.TryGetValue("discipline", out var discipline) && !string.IsNullOrWhiteSpace(discipline)
                    || e.Metadata.TryGetValue("disciplines", out var disciplines) && !string.IsNullOrWhiteSpace(disciplines))
                    )
                )
            {
                var path = CategorizeAndBuildPath("Other", "Crafting", startKey, startNode, currentKey, parent);
                return (currentKey, current, path);
            }

            var foundTokenCriteria = tokenCriteria.Where(c => edges.Any(e => c.Criteria.Matches(e.To))).ToList();
            if (foundTokenCriteria.Count > 0)
            {
                foreach (var criteria in foundTokenCriteria)
                {
                    var groupName = criteria.Group?.Name;
                    var categoryName = criteria.Category?.Name ?? "";
                    var groupOfCategoryName = criteria.GroupOfCategoryName ?? "";
                    if(groupName != null)
                    {
                        var path = CategorizeAndBuildPath(groupName, null, startKey, startNode, currentKey, parent);
                        return (currentKey, current, path);
                    }
                    else
                    {
                        var path = CategorizeAndBuildPath(groupOfCategoryName, categoryName, startKey, startNode, currentKey, parent);
                        return (currentKey, current, path);
                    }
                }
            }

            foreach (var edge in edges)
            {
                if (edge.Type == EdgeType.HasIngredient && edge.Metadata != null && edge.Metadata.TryGetValue("source", out var recipeSource) && recipeSource.Equals("mystic forge", StringComparison.OrdinalIgnoreCase))
                {
                    var path = CategorizeAndBuildPath("Other", "Mystic Forge", startKey, startNode, currentKey, parent);
                    return (currentKey, current, path);
                }

                var nextCost = searchState.Cost;
                // If SoldBy → capture cost
                if (edge.Type == EdgeType.SoldBy &&
                    edge.Metadata != null && edge.Metadata.TryGetValue("cost", out var cost))
                {
                    nextCost = cost; // overwrite or store
                }

                if (visited.Add(edge.To))
                {
                    parent[edge.To] = currentKey;

                    queue.Enqueue(new SearchState
                    {
                        Key = edge.To,
                        Cost = nextCost,
                        IncomingEdgeType = edge.Type
                    });
                }
            }

            if (current.Type == NodeType.Weapon && current.Metadata.TryGetValue("IsNamedExoticWeapon", out var rarity) && rarity.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                var path = CategorizeAndBuildPath("Other", "General", startKey, startNode, currentKey, parent);
                return (currentKey, current, path);
            }
        }

        return null;
    }

    private static List<string> CategorizeAndBuildPath(string groupName, string? categoryName, string startKkey, Node startNode, string currentKey, Dictionary<string, string?> parent)
    {
        categoryName ??= "";

        var group = classifyConfig.UnlockGroups.Single(g => g.Name.Equals(groupName, StringComparison.Ordinal));
        if (categoryName == null)
        {
            group.Unlocks.Add(new Unlock(startKkey, startNode));
        }
        else
        {
            var category = group.UnlockCategories.Single(c => c.Name.Equals(categoryName, StringComparison.Ordinal));
            category.Unlocks.Add(new Unlock(startKkey, startNode));
        }
        var path = BuildPath(currentKey, parent);
        path.Add($"{group.Name}:{categoryName}");
        return path;
    }

    private static List<string> BuildPath(string endKey, Dictionary<string, string?> parent)
    {
        var path = new List<string>();
        var current = endKey;

        while (current != null)
        {
            path.Add(current);
            current = parent[current];
        }

        path.Reverse();
        return path;
    }
}
