using Gw2Unlocks.Wiki;
using Microsoft.Extensions.Logging;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gw2Unlocks.WikiProcessing.Implementation;

public sealed class Gw2WikiProcessingSource(
    ILogger<Gw2WikiProcessingSource> logger,
    IGw2WikiCache wikiCache) : IGw2WikiProcessingSource
{
    public async Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken)
    {
        var graph = new AcquisitionGraph();
        var parser = new WikitextParser();

        await foreach (var xml in wikiCache.StreamAllPages(cancellationToken))
        {
            try
            {
                foreach (var (title, text) in ExtractPages([xml]))
                {
                    try
                    {
                        var ast = parser.Parse(text, cancellationToken);
                        
                        List<string> debugtitles = [
                            //"Plush Zhaia Backpack (skin)",
                            //"Plush Zhaia Backpack",
                            //"Halloween Vendor",
                            //"Hooligan's Route",
                            //"Lion's Arch",
                            //"Mini Exalted Sage",
                            //"Exalted Mastery Vendor",
                            //"Tarir, the Forgotten City",
                            //"Auric Basin",
                            //"Axe of the Dragon's Deep",
                            //"Arah Weapons Box",
                            //"Dungeon Weapon Container",
                            ];

                        //logger.LogInformation(
                        //    "TITLE RAW: [{title}] LENGTH: {len} CHARS: {chars}",
                        //    title,
                        //    title.Length,
                        //    string.Join(",", title.Select(c => (int)c))
                        //);
                        if (debugtitles.Contains(title.Trim()))
                            logger.LogDebug("debug thing found {title}", title);
                        
                        var infobox = ParseInfobox(ast);
                        if (infobox == null || infobox.Metadata.TryGetValue("status", out var status) && status == "historical")
                            continue;

                        var nodeType = MapNodeType(infobox.InfoBoxType);
                        if (nodeType == NodeType.None)
                        {
                            var nodeAlreadyInGraph = graph.GetNode(title);
                            if (nodeAlreadyInGraph != null && nodeAlreadyInGraph.Type == NodeType.None)
                                graph.RemoveNodeAndAllEdges(title);
                            logger.LogDebug("Skipping {title} {infoboxtype}", title, infobox.InfoBoxType);
                            continue;
                        }

                        var node = graph.GetOrCreate(title, infobox.Metadata);                        
                        node.SetType(nodeType);

                        ApplyRelationships(graph, title, node, ast, infobox);
                        logger.LogInformation("Processed wiki page {title}", title);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing wiki page {title}", title);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing wiki page XML {xml}", xml);
            }
        }

        graph.Nodes.Where(kv => kv.Value.Type == NodeType.None)
            .ToList()
            .ForEach(kv => logger.LogWarning("Node with no type: {title}", kv.Key));

        return graph;
    }

    // -------------------------
    // INFBOX PARSING
    // -------------------------
    private sealed class InfoboxData
    {
        public string InfoBoxType { get; init; } = "";
        public Dictionary<string, string> Metadata { get; init; } = [];

        public string? Get(string key)
            => Metadata.TryGetValue(key, out var v) ? v : null;
    }

    private static InfoboxData? ParseInfobox(Wikitext ast)
    {
        var template = ast.EnumDescendants()
            .OfType<Template>()
            .FirstOrDefault(t => t.Name.ToString().Contains("infobox", StringComparison.OrdinalIgnoreCase));

        if (template == null)
            return null;

        var rawName = template.Name.ToString().Trim();

        var typeName = rawName
            .Replace("infobox", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in template.Arguments)
        {
            var key = GetText(arg.Name);
            var value = GetText(arg.Value);

            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                metadata[key] = value;
            }
        }

        var namedExoticWeapon = ast.EnumDescendants()
            .OfType<Template>()
            .FirstOrDefault(t => t.Name.ToString().Contains("exotic weapon text", StringComparison.OrdinalIgnoreCase));
        if(namedExoticWeapon != null)
        {
            metadata["IsNamedExoticWeapon"] = "true";
        }

        return new InfoboxData
        {
            InfoBoxType = typeName,
            Metadata = metadata
        };
    }

    private static NodeType MapNodeType(string typeName)
    {
        return typeName.ToUpperInvariant() switch
        {
            "ITEM" => NodeType.Item,
            "NPC" => NodeType.NPC,
            "SKIN" => NodeType.Skin,
            "LOCATION" => NodeType.Location,

            "WEAPON" => NodeType.Weapon,
            "ARMOR" => NodeType.Armor,
            "BACK ITEM" => NodeType.BackItem,


            "OBJECT" => NodeType.Gw2Object,

            _ => NodeType.None
        };
    }

    // -------------------------
    // RELATIONSHIPS
    // -------------------------
    private static void ApplyRelationships(
        AcquisitionGraph graph,
        string nodeId,
        Node node,
        Wikitext ast,
        InfoboxData info)
    {
        // 🔹 Skin page override
        if (info.InfoBoxType.Equals("Skin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 🔹 Skin link
        var skinData = info.Get("skin");
        if (!string.IsNullOrWhiteSpace(skinData))
        {
            skinData.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ForEach(skin =>
                {

                    var skinNode = graph.GetOrCreate(skin);
                    skinNode.SetType(NodeType.Skin);

                    graph.AddEdge(skin, nodeId, EdgeType.SkinUnlock);
                });
        }

        // 🔹 Location hierarchy
        if (info.InfoBoxType.Equals("Location", StringComparison.OrdinalIgnoreCase))
        {
            var within = info.Get("within");

            if (!string.IsNullOrWhiteSpace(within))
            {
                foreach (var parent in within.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    graph.GetOrCreate(parent);
                    graph.AddEdge(nodeId, parent, EdgeType.LocatedIn);
                }
            }
        }


        // 🔹 objects
        if (info.InfoBoxType.Equals("Object", StringComparison.OrdinalIgnoreCase))
        {
            HandleLocation(graph, nodeId, info);
        }

        // 🔹 NPC that are merchants (Vendor)
        if (info.InfoBoxType.Equals("NPC", StringComparison.OrdinalIgnoreCase))
        {
            if (info.Get("service")?.Contains("merchant", StringComparison.OrdinalIgnoreCase) == true)
            {
                HandleLocation(graph, nodeId, info);

                foreach (var row in ast.EnumDescendants().OfType<Template>()
                             .Where(t => t.Name.ToString().Contains("vendor table row", StringComparison.OrdinalIgnoreCase)))
                {
                    var itemName = row.Arguments.FirstOrDefault(a => a.Name?.ToString() == "item")?.Value?.ToString();
                    var cost = row.Arguments.FirstOrDefault(a => a.Name?.ToString() == "cost")?.Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        graph.GetOrCreate(itemName);

                        var metadata = !string.IsNullOrWhiteSpace(cost)
                            ? new Dictionary<string, string> { ["cost"] = cost }
                            : null;

                        graph.AddEdge(itemName, nodeId, EdgeType.SoldBy, metadata);
                    }
                }
            }
        }

        // 🔹 Container (metadata-driven)
        if (info.Get("type")?.Equals("Container", StringComparison.OrdinalIgnoreCase) == true)
        {
            node.SetType(NodeType.Container);

            foreach (var template in ast.EnumDescendants().OfType<Template>())
            {
                if (!template.Name.ToString().Equals("contains", StringComparison.OrdinalIgnoreCase))
                    continue;

                var target = template.Arguments
                    .FirstOrDefault(a => a.Name == null || string.IsNullOrWhiteSpace(a.Name.ToString()))
                    ?.Value?.ToString()?.Trim();

                if (!string.IsNullOrWhiteSpace(target))
                {
                    graph.GetOrCreate(target);
                    graph.AddEdge(target, nodeId, EdgeType.ContainedIn);
                }
            }
        }

        // 🔹 Gathered from Object
        if (info.InfoBoxType.Equals("Object", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var template in ast.EnumDescendants().OfType<Template>())
            {
                if (!template.Name.ToString().Equals("gather", StringComparison.OrdinalIgnoreCase))
                    continue;

                // first unnamed parameter = item name
                var itemName = template.Arguments
                    .FirstOrDefault(a => a.Name == null || string.IsNullOrWhiteSpace(a.Name.ToString()))
                    ?.Value?.ToString()?.Trim();

                if (!string.IsNullOrWhiteSpace(itemName))
                {
                    graph.GetOrCreate(itemName);
                    graph.AddEdge(
                        itemName,
                        nodeId,
                        EdgeType.GatheredFrom);
                }
            }
        }

        ApplyCrafting(graph, nodeId, ast);
    }

    private static void HandleLocation(AcquisitionGraph graph, string nodeId, InfoboxData info)
    {
        var locations = info.Get("location")?
                        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (locations != null)
        {
            foreach (var loc in locations)
            {
                graph.GetOrCreate(loc);
                graph.AddEdge(nodeId, loc, EdgeType.LocatedIn);
            }
        }
    }

    // -------------------------
    // CRAFTING
    // -------------------------
    private static void ApplyCrafting(
        AcquisitionGraph graph,
        string outputNodeId,
        Wikitext ast)
    {
        foreach (var template in ast.EnumDescendants().OfType<Template>())
        {
            var name = template.Name.ToString().Trim();
            var props = GetRecipeProperties(template);

            // -------------------------
            // RECIPE
            // -------------------------
            if (name.Equals("recipe", StringComparison.OrdinalIgnoreCase))
            {
                var status = GetArg(template, "status");

                // ❌ skip historical
                if (status?.Equals("historical", StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                foreach (var ingredient in GetIngredients(template))
                {
                    graph.GetOrCreate(ingredient);
                    graph.AddEdge(
                        outputNodeId,
                        ingredient,
                        EdgeType.HasIngredient, props);
                }
            }

            // -------------------------
            // CRAFT TABLE
            // -------------------------
            else if (name.Equals("craft table", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var ingredient in GetIngredients(template))
                {
                    graph.GetOrCreate(ingredient);
                    graph.AddEdge(
                        outputNodeId,
                        ingredient,
                        EdgeType.HasIngredient, props);
                }
            }
        }
    }

    // -------------------------
    // HELPERS
    // -------------------------
    private static Dictionary<string, string> GetRecipeProperties(Template template)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in template.Arguments)
        {
            var key = arg.Name?.ToString().Trim();
            var value = arg.Value?.ToString().Trim();

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                continue;

            // Skip ingredients
            if (key.StartsWith("ingredient", StringComparison.OrdinalIgnoreCase))
                continue;

            result[key] = value;
        }

        return result;
    }

    private static IEnumerable<string> GetIngredients(Template template)
    {
        foreach (var arg in template.Arguments)
        {
            var key = arg.Name?.ToString().Trim();

            if (key == null || !key.StartsWith("ingredient", StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = arg.Value?.ToString();
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            // "25 Candy Cane" → "Candy Cane"
            var parts = raw.Trim().Split(' ', 2);

            yield return parts.Length == 2 ? parts[1] : parts[0];
        }
    }

    private static string? GetArg(Template template, string name)
    {
        return template.Arguments
            .FirstOrDefault(a => a.Name?.ToString().Equals(name, StringComparison.OrdinalIgnoreCase) == true)
            ?.Value?.ToString();
    }

    private static string GetText(Wikitext? node)
    {
        if (node == null) return string.Empty;

        return string.Concat(node.EnumDescendants()
            .OfType<PlainText>()
            .Select(pt => pt.ToPlainText()))
            .Trim();
    }

    private static List<(string title, string text)> ExtractPages(IEnumerable<string> xmls)
    {
        var pages = new List<(string, string)>();

        foreach (var xml in xmls)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://www.mediawiki.org/xml/export-0.11/";

            foreach (var page in doc.Descendants(ns + "page"))
            {
                var title = page.Element(ns + "title")?.Value;
                var text = page.Descendants(ns + "text").FirstOrDefault()?.Value;

                if (!string.IsNullOrWhiteSpace(title) &&
                    !string.IsNullOrWhiteSpace(text))
                {
                    pages.Add((title, text));
                }
            }
        }

        return pages;
    }

    public async Task<ZoneData> GetZoneData(CancellationToken cancellationToken)
    {
        var zonePageString = await wikiCache.GetSinglePage("zone", cancellationToken);

        if (string.IsNullOrWhiteSpace(zonePageString))
        {
            return new ZoneData { Zones = [] };
        }

        var zones = zonePageString
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("{{Post-launch zone table row", StringComparison.Ordinal))
            .Select(ParseZone)
            .ToList();

        return new ZoneData { Zones = new Collection<Zone>(zones) };
    }

    private static Zone ParseZone(string line)
    {
        var content = line
            .Trim()
            .TrimStart('{')
            .TrimEnd('}');

        var parts = content.Split('|');

        var zoneName = parts[1].Trim();

        string? achievementsRaw = null;

        // First pass: explicit achievements=
        foreach (var part in parts.Skip(2))
        {
            if (part.StartsWith("achievements=", StringComparison.OrdinalIgnoreCase))
            {
                achievementsRaw = part["achievements=".Length..];
                break;
            }
        }

        // Second pass: positional achievement (only if no achievements=)
        if (achievementsRaw == null)
        {
            foreach (var part in parts.Skip(3)) // skip: template, zone, type
            {
                if (!part.Contains('=', StringComparison.Ordinal)) // positional param
                {
                    achievementsRaw = part;
                    break;
                }
            }
        }

        var achievements = ParseAchievements(zoneName, achievementsRaw);

        return new Zone(zoneName, new Collection<string>([.. achievements]));
    }

    private static string[] ParseAchievements(string zoneName, string? raw)
    {
        if (raw == null)
        {
            // fallback
            return new[] { zoneName };
        }

        if (raw.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return [.. raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a
                .Replace(" (achievements)", "", StringComparison.OrdinalIgnoreCase)
                .Trim())];
    }
}