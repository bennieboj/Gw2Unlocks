using Gw2Unlocks.Api;
using Gw2Unlocks.WikiGraph;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.UnlockClassifier;

public class Classifier(/*IGw2ApiSource apiSource,*/ IGw2WikiGraphSource wikigraphSource, ILogger<Classifier> logger) : IClassifier
{
    private readonly Lazy<Task<AcquisitionGraph>> _graph = new(() => wikigraphSource.GetAcquisitionGraph());
    private readonly Dictionary<string, Collection<string>> _linkZoneToCurrencies = new()
    {
            { "Verdant Brink", new Collection<string> { "Airship Part" } },
            { "Auric Basin", new Collection<string> { "Lump of Aurillium" } }
    };

    //private readonly Dictionary<string, Collection<string>> _linkAchievementCategoryTo = new()
    //{
    //        { "Verdant Brink", new Collection<string> { "Airship Part" } },
    //        { "Auric Basin", new Collection<string> { "Lump of Aurillium" } }
    //};

    //Gw2Unlock.Collection
    // - Gw2Unlock.Category
    //   - Gw2Unlocks.HowToUnlock
    //HoT
    // - Vderant Brink
    //   - maps: VB
    //   - achievements: 
    // - Auric Basin
    //PoF, EoD, LW1, LW2, LS1, LS2, LS3, LS4, Icebrood Saga, Festivals, Raids, Dungeons, Cities
    //Other
    // - Black Lion Statuette


    public async Task<string> ClassifyUnlock(string unlock, CancellationToken cancellationToken)
    {
        var graph = await _graph.Value;
        var result = FindZoneOrCity(graph, unlock);
        if (result != null)
        {
            var zone = result.Value;
            logger.LogInformation("{zone.Key} ({zone.Node.Type})", zone.Key, zone.Node.Type);

            logger.LogInformation("Path:");
            foreach (var step in zone.Path)
            {
                logger.LogInformation("  -> {step}", step);
            }
            return zone.Key;
        }
        return "";
    }

    private static bool ShouldClassify(Node node) =>
    node switch
    {
        { Type: NodeType.Skin } => true,
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

    public async Task ClassifyUnlocks(CancellationToken cancellationToken = default)
    {
        var graph = await _graph.Value;

        var results = new Dictionary<string, string>();
        foreach (var (key, node) in graph.Nodes)
        {
            if (!ShouldClassify(node))
            {
                logger.LogDebug("Skipping {key} ({type})", key, node.Type);
                continue;
            }
            logger.LogInformation("Finding {key} ({type})", key, node.Type);

            var result = await ClassifyUnlock(key, cancellationToken);

            if (string.IsNullOrEmpty(result))
            {
                logger.LogError("missing {key}", key);
            }
            else
            {
                results[key] = result;
            }
            if (results.Count > 10)
            {
                //logger.LogInformation("Classified {count} unlocks so far...", results.Count);
                break;
            }
        }




        //var items = await apiSource.GetItemsAsync(cancellationToken);
        //var achievements = await apiSource.GetAchievementsAsync(cancellationToken);
        //var titles = await apiSource.GetTitlesAsync(cancellationToken);
        //var novelties = await apiSource.GetNoveltiesAsync(cancellationToken);
        //var miniatures = await apiSource.GetMiniaturesAsync(cancellationToken);
        //logger.LogInformation("Loaded from API cache: " +
        //        "{itemsCount} items, " + 
        //        "{achievementsCount} achievements, " +
        //        "{titlesCount} titles, " +
        //        "{noveltiesCount} novelties, " +
        //        "{miniaturesCount} miniatures.",
        //        items.Count,
        //        achievements.Count,
        //        titles.Count,
        //        novelties.Count,
        //        miniatures.Count
        //        );
    }

    private readonly List<string> debugtitles = [
        //"Plush Zhaia Backpack (skin)",
        //"Plush Zhaia Backpack",
        //"Halloween Vendor",
        //"Hooligan's Route",
        //"Lion's Arch",
        "Mini Exalted Sage",
        "Exalted Mastery Vendor",
        "Noble Ledges",
        "Verdant Brink",
        "Noble's Folly",
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
    }

    private bool IsValidForZone(string zone, string? cost)
    {
        if (cost == null)
            return true;

        if (!_linkZoneToCurrencies.TryGetValue(zone, out var validCurrencies))
            return true;

        return validCurrencies.Any(c =>
            cost.Contains(c, StringComparison.OrdinalIgnoreCase));
    }

    private (string Key, Node Node, List<string> Path)? FindZoneOrCity(
        AcquisitionGraph graph,
        string startKey)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<SearchState>();
        var parent = new Dictionary<string, string?>();

        visited.Add(startKey);
        queue.Enqueue(new SearchState
        {
            Key = startKey,
            Cost = null
        });
        parent[startKey] = null;

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            var currentKey = state.Key;
            var current = graph.GetNode(currentKey);

            if(debugtitles.Contains(currentKey) && false)
            {
                logger.LogInformation("Visiting {key} ({type}) with cost {cost}", currentKey, current?.Type, state.Cost);
            }

            if (current == null)
                continue;

            // When we reach a zone/city → validate cost
            if (current.Type == NodeType.Location &&
                current.Metadata.TryGetValue("type", out var value) &&
                (value.Equals("Zone", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("City", StringComparison.OrdinalIgnoreCase)))
            {
                if (IsValidForZone(currentKey, state.Cost))
                {
                    var path = BuildPath(currentKey, parent);
                    return (currentKey, current, path);
                }
            }

            foreach (var edge in graph.Edges.Where(e => e.From == currentKey))
            {
                var nextCost = state.Cost;

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
                        Cost = nextCost
                    });
                }
            }
        }

        return null;
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
