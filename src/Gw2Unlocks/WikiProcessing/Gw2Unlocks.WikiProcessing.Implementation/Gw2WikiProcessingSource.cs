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
    private const string gemStorePage = "Gem Store/data";

    private const string blackLionWeaponsSpecialistNPCKey = "Black Lion Weapons Specialist";
    private readonly List<string> blackLionClaimTicketPages = ["Black Lion Weapons Specialist/historical", "Template:Inventory/black lion claim ticket", "Black Lion Weapons Specialist (Halloween)"];
    private readonly List<string> blackLionStatuettePages = ["Black Lion Statuette/historical", "Template:Inventory/statuette"];

    public async Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken)
    {
        var graph = new AcquisitionGraph();
        var parser = new WikitextParser();

        await RunParsingPass(graph, parser, FirstPass, cancellationToken);
        await RunParsingPass(graph, parser, SecondPass, cancellationToken);
        ApplyBlackLion(graph);
        graph.Nodes.Where(kv => kv.Value.Type == NodeType.None)
            .ToList()
            .ForEach(kv => logger.LogWarning("Node with no type: {title}", kv.Key));

        return graph;
    }

    private static void ApplyBlackLion(AcquisitionGraph graph)
    {
        foreach (var item in graph.Nodes.ToList())
        {
            if ((item.Value.Type == NodeType.Item || item.Value.Type == NodeType.Skin) && item.Value.Metadata.TryGetValue("collection", out var collection))
            {
                var x = graph.GetNode(collection, NodeType.BlackLionWeaponCollection);
                if (x != null)
                {
                    graph.AddEdge(item.Key, blackLionWeaponsSpecialistNPCKey, EdgeType.SoldBy, x.Metadata);
                }
            }
        }
    }

    private async Task RunParsingPass(AcquisitionGraph graph, WikitextParser parser, Action<AcquisitionGraph, string, Wikitext> pass, CancellationToken cancellationToken)
    {
        await foreach (var xml in wikiCache.StreamAllPages(cancellationToken))
        {
            try
            {
                foreach (var (title, text) in ExtractPages([xml]))
                {
                    try
                    {
                        var textCleaned = text;
                        if (title.Contains("Template:", StringComparison.Ordinal))
                        {
                            textCleaned = textCleaned.Replace("<onlyinclude>", "", StringComparison.Ordinal);
                        }

                        List<string> debugtitles = [
                            //"Plush Zhaia Backpack (skin)",
                            "Piles of Bloodstone Dust",
                            "Pile of Bloodstone Dust",
                            "Abaddon's Axe"
                            ];

                        //logger.LogInformation(
                        //    "TITLE RAW: [{title}] LENGTH: {len} CHARS: {chars}",
                        //    title,
                        //    title.Length,
                        //    string.Join(",", title.Select(c => (int)c))
                        //);
                        if (debugtitles.Contains(title.Trim()))
                            logger.LogDebug("debug thing found {title}", title);

                        var ast = parser.Parse(textCleaned, cancellationToken);

                        pass.Invoke(graph, title, ast);

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
    }


    private static void FirstPass(AcquisitionGraph graph, string title, Wikitext ast)
    {
        ParseAndAssignRedirects(graph, title, ast);
    }
    private void SecondPass(AcquisitionGraph graph, string title, Wikitext ast)
    {
        if(IsRedirect(ast))
        {
            return;
        }

        var infobox = ParseInfobox(ast);
        if (title.Contains(gemStorePage, StringComparison.Ordinal))
        {
            ParseGemStoreEntries(graph, ast);
            return;
        }
        else if (blackLionClaimTicketPages.Any(pageTitle => title.Equals(pageTitle, StringComparison.Ordinal)))
        {
            ParseBlackLionClaimTicketEntries(graph, ast);
            return;
        }
        else if (blackLionStatuettePages.Any(pageTitle => title.Equals(pageTitle, StringComparison.Ordinal)))
        {
            ParseBlackLionStatuetteEntries(graph, ast);
            return;
        }
        else if (infobox == null || infobox.Metadata.TryGetValue("status", out var status) && status == "historical")
        {
            return;
        }
        if (infobox.InfoBoxType.Equals("Achievement category", StringComparison.OrdinalIgnoreCase))
        {
            ApplyAchievements(graph, title, ast, infobox);
            logger.LogInformation("Processed achievement category page {title}", title);
            return;
        }

        var nodeType = MapNodeType(infobox.InfoBoxType);
        if (nodeType == NodeType.None)
        {
            var nodeAlreadyInGraph = graph.GetNode(title);
            if (nodeAlreadyInGraph != null && nodeAlreadyInGraph.Type == NodeType.None)
                graph.RemoveNodeAndAllEdges(title);
            logger.LogDebug("Skipping {title} {infoboxtype}", title, infobox.InfoBoxType);
            return;
        }

        var node = graph.GetOrCreate(title, infobox.Metadata);
        node.SetType(nodeType);

        ApplyRelationships(graph, title, node, ast, infobox);
    }

    private static bool ParseAndAssignRedirects(AcquisitionGraph graph, string title, Wikitext ast)
    {
        var target = DetectRedirect(ast);
        if (target != null)
            graph.CreateRedirect(title, target);
        return target != null;
    }
    private static bool IsRedirect(Wikitext ast)
    {
        var target = DetectRedirect(ast);
        return target != null;
    }

    private static string? DetectRedirect(Wikitext ast)
    {
        return ast.Lines.OfType<InlineContainerLineNode>()
                              .SelectMany(l => l.Inlines)
                              .Select(il => new { il, link = il as WikiLink })
                              .Where(x =>
                                x.link != null &&
                                x.il.PreviousNode is PlainText prev &&
                                prev.Content.Trim().Equals("REDIRECT", StringComparison.Ordinal) &&
                                !string.IsNullOrWhiteSpace(x.link.Target?.ToString())
                                )
                              .Select(x => x.link!.Target!.ToString())
                              .FirstOrDefault();
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

    private static void ParseGemStoreEntries(AcquisitionGraph graph, Wikitext ast)
    {
        var gemStoreEntries = ast.EnumDescendants()
            .OfType<Template>()
            .Where(t => t.Name.ToString().Contains("Gem store entry", StringComparison.OrdinalIgnoreCase));

        const string gemStoreNpcKey = "Gem Store";
        var gemstore = graph.GetNode(gemStoreNpcKey);
        if (gemstore == null)
        {
            gemstore = new Node(new Dictionary<string, string> {
                { "service", "merchant" }
            });
            gemstore.SetType(NodeType.NPC);
            graph.Nodes.Add(gemStoreNpcKey, gemstore);
        }

        foreach (var gemstoreEntry in gemStoreEntries)
        {
            string? itemName = null;
            string? cost = null;
            string? availability = null;
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var arg in gemstoreEntry.Arguments)
            {
                var key = GetText(arg.Name);
                var value = GetText(arg.Value);

                if (key.Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    itemName = value;
                    continue;
                }
                else if (key.Equals("cost", StringComparison.OrdinalIgnoreCase))
                {
                    cost = value;
                    continue;
                }
                else if (key.Equals("availability", StringComparison.OrdinalIgnoreCase))
                {
                    availability = value;
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    metadata[key] = value;
                }
            }
            if (itemName == null || cost == null || availability == null || availability.Equals("historical", StringComparison.OrdinalIgnoreCase))
                continue;
            metadata.Add("cost", cost + " Gems");
            graph.AddEdge(itemName, gemStoreNpcKey, EdgeType.SoldBy, metadata);
        }
    }

    private static void ParseBlackLionClaimTicketEntries(AcquisitionGraph graph, Wikitext ast)
    {
        var tablerows = ast.EnumDescendants().OfType<Template>()
                             .Where(t => t.Name.ToString().Trim().Contains("vendor table row", StringComparison.OrdinalIgnoreCase));
        foreach (var row in tablerows)
        {
            var itemName = row.Arguments.FirstOrDefault(a => a.Name?.ToString().Trim() == "item")?.Value?.ToString().Trim();
            var cost = row.Arguments.FirstOrDefault(a => a.Name?.ToString().Trim() == "cost")?.Value?.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(itemName))
            {
                graph.GetOrCreate(itemName);

                var metadata = !string.IsNullOrWhiteSpace(cost)
                    ? new Dictionary<string, string> { ["cost"] = cost }
                    : null;

                graph.AddEdge(itemName, blackLionWeaponsSpecialistNPCKey, EdgeType.SoldBy, metadata);
            }
        }


        var tables = ast.EnumDescendants().OfType<Template>()
                             .Where(t => t.Name.ToString().Trim().Contains("Vendor table (Black Lion Weapons)", StringComparison.OrdinalIgnoreCase));
        foreach (var table in tables)
        {
            var collectionName = table.Arguments.FirstOrDefault(a => a.Name?.ToString().Trim() == "collection")?.Value?.ToString().Trim();
            var cost = table.Arguments.FirstOrDefault(a => a.Name?.ToString().Trim() == "cost")?.Value?.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(collectionName))
            {
                var metadata = !string.IsNullOrWhiteSpace(cost)
                    ? new Dictionary<string, string> { ["cost"] = cost + " Black Lion Claim Ticket" }
                    : null;
                var node = graph.GetOrCreate(collectionName + " Weapon Collection", metadata);
                node.SetType(NodeType.BlackLionWeaponCollection);
            }
        }
    }

    private static void ParseBlackLionStatuetteEntries(AcquisitionGraph graph, Wikitext ast)
    {
        var gemStoreEntries = ast.EnumDescendants()
            .OfType<Template>()
            .Where(t => t.Name.ToString().Contains("vendor table row", StringComparison.OrdinalIgnoreCase));

        const string blackLionChestMerchantNpcKey = "Black Lion Chest Merchant";
        var gemstore = graph.GetNode(blackLionChestMerchantNpcKey);
        if (gemstore == null)
        {
            gemstore = new Node(new Dictionary<string, string> {
                { "service", "merchant" }
            });
            gemstore.SetType(NodeType.NPC);
            graph.Nodes.Add(blackLionChestMerchantNpcKey, gemstore);
        }

        foreach (var gemstoreEntry in gemStoreEntries)
        {
            string? itemName = null;
            string? cost = null;
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var arg in gemstoreEntry.Arguments)
            {
                var key = GetText(arg.Name);
                var value = GetText(arg.Value);

                if (key.Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    itemName = value;
                    continue;
                }
                else if (key.Equals("cost", StringComparison.OrdinalIgnoreCase))
                {
                    cost = value;
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    metadata[key] = value;
                }
            }
            if (itemName == null || cost == null)
                continue;
            metadata.Add("cost", cost);
            graph.AddEdge(itemName, blackLionChestMerchantNpcKey, EdgeType.SoldBy, metadata);
        }
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
            "GEM STORE COMBO" => NodeType.GemStoreCombo,
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

    private static void ApplyAchievements(
    AcquisitionGraph graph,
    string title,
    Wikitext ast,
    InfoboxData info)
    {
        if (!info.InfoBoxType.Equals("Achievement category", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var template in ast.EnumDescendants().OfType<Template>())
        {
            if (!template.Name.ToString().Trim()
                .Equals("Achievement table row", StringComparison.OrdinalIgnoreCase))
                continue;

            var id = GetArg(template, "id");
            var name = GetArg(template, "name");

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                continue;

            var key = $"{title}#achievement{id}";

            var node = graph.GetOrCreate(key, new Dictionary<string, string>
            {
                ["name"] = name,
                ["achievementId"] = id,
                ["category"] = title
            });

            node.SetType(NodeType.Achievement);
        }
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
            string? vendorHeaderLocation = null;
            var vendorHeaders = ast.EnumDescendants().OfType<Template>().Where(t => t.Name.ToString().Contains("vendor table header", StringComparison.OrdinalIgnoreCase)).ToList();
            if(vendorHeaders.Count == 1)
            {
                var header = vendorHeaders[0];
                vendorHeaderLocation = header.Arguments.FirstOrDefault(a => string.Equals(a.Name?.ToString()?.Trim(),"location", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
            }

            if (!string.IsNullOrWhiteSpace(info.Get("service")))
            {
                HandleLocation(graph, nodeId, info, vendorHeaderLocation);

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
        var isContainer = info.Get("type")?.Equals("Container", StringComparison.OrdinalIgnoreCase);
        if (isContainer == true || node.Type == NodeType.GemStoreCombo)
        {
            if(isContainer == true)
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

    private static void HandleLocation(AcquisitionGraph graph, string nodeId, InfoboxData info, string? allowedLocations = null)
    {
        var locations = info.Get("location")?
                        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if(allowedLocations != null)
        {
            var allowed = allowedLocations
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            locations = locations?.Where(l => allowed.Contains(l)).ToArray();
        }

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
            .FirstOrDefault(a => a.Name?.ToString().Trim().Equals(name, StringComparison.OrdinalIgnoreCase) == true)
            ?.Value?.ToString().Trim();
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
            return [zoneName];
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