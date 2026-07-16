using Gw2Unlocks.Wiki;
using Microsoft.Extensions.Logging;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gw2Unlocks.WikiProcessing.Implementation;

public sealed class Gw2WikiProcessingSource(
    ILogger<Gw2WikiProcessingSource> logger,
    IGw2WikiCache wikiCache) : IGw2WikiProcessingSource
{
    private readonly List<string> gemStorePages = ["Gem Store/data", "Gem Store/data (historical)"];

    private const string blackLionWeaponsSpecialistNPCKey = "Black Lion Weapons Specialist";
    private readonly List<string> blackLionClaimTicketPages = ["Black Lion Weapons Specialist/historical", "Template:Inventory/black lion claim ticket", "Black Lion Weapons Specialist (Halloween)"];
    private readonly List<string> blackLionStatuettePages = ["Black Lion Statuette/historical", "Template:Inventory/statuette"];
    private readonly WikitextParser parser = new();

    public async Task<AcquisitionGraph> GetAcquisitionGraph(CancellationToken cancellationToken)
    {
        var graph = new AcquisitionGraph();

        await RunParsingPass(graph, FirstPass, cancellationToken);
        await RunParsingPass(graph, SecondPass, cancellationToken);
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

    private async Task RunParsingPass(AcquisitionGraph graph, Action<AcquisitionGraph, string, string, Wikitext, CancellationToken> pass, CancellationToken cancellationToken)
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
                        if (IsTemplate(title))
                        {
                            textCleaned = CleanTemplate(text);
                        }

                        List<string> debugtitles = [
                            //"Plush Zhaia Backpack (skin)",
                            "Piles of Bloodstone Dust",
                            "Pile of Bloodstone Dust",
                            "Abaddon's Axe",
                            "Illustrious Breastplate"
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

                        pass.Invoke(graph, title, text, ast, cancellationToken);

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


    private static void FirstPass(AcquisitionGraph graph, string title, string text, Wikitext ast, CancellationToken _)
    {
        ParseAndAssignRedirects(graph, title, ast);
        ParseAndAssignTemplates(graph, title, text);
        ParseAndAssignSubPage(graph, title, text);
        ParseAndAssignArmorSets(graph, title, ast);
    }
    private void SecondPass(AcquisitionGraph graph, string title, string _, Wikitext ast, CancellationToken cancellationToken)
    {
        if(IsRedirect(ast))
        {
            return;
        }

        var infobox = ParseInfobox(ast);
        if (gemStorePages.Any(pageTitle => title.Equals(pageTitle, StringComparison.Ordinal)))
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
        else if (IsTemplate(title))
        {
            //gems, bltc and staettue pages contain templates
            return;
        }
        else if (IsSubPage(title))
        {
            //subpages on it's own aren't useful
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

        ApplyRelationships(graph, title, node, ast, infobox, cancellationToken);
    }

    private static void ParseAndAssignArmorSets(AcquisitionGraph graph, string title, Wikitext ast)
    {
        if (IsArmorSet(ast) is Template armorSetTemplate)
        {
            var setName = armorSetTemplate.Arguments.FirstOrDefault(a => a.Name?.ToString().Trim() == "name")?.Value?.ToString().Trim();
            if (setName != null)
            {
                //kind of a hack
                graph.CreateRedirect(title, setName);
            }
        }
    }

    private static Template? IsArmorSet(Wikitext ast)
    {
       return ast.EnumDescendants()
            .OfType<Template>()
            .FirstOrDefault(t => t.Name.ToString().Trim().Equals("Armor set infobox", StringComparison.OrdinalIgnoreCase));
    }

    private static void ParseAndAssignSubPage(AcquisitionGraph graph, string title, string text)
    {
        if (IsSubPage(title))
        {
            graph.CreateSubPage(title, text);
        }
    }
    private static bool IsSubPage(string title)
    {
        var testTitle = title[0] != '"' ? title[1..] : title[2..];
        var isSubPage = testTitle.Contains('/', StringComparison.OrdinalIgnoreCase);
        var bad = new List<string> { 
            "/history", "/Drop rate", "/data", "/historical", "Game updates/", "/quotes",
            "/dialogue", "/research", "/locations", "/Salvage research"
        };
        var containsBadThings =  bad.Any(b => title.Contains(b, StringComparison.OrdinalIgnoreCase));
        return isSubPage && !containsBadThings && !IsTemplate(title);
    }

    private static void ParseAndAssignTemplates(AcquisitionGraph graph, string title, string text)
    {
        if (IsTemplate(title))
        {
            graph.CreateTemplate(title, CleanTemplate(text));
        }
    }

    private static string CleanTemplate(string text)
    {
        return text.Replace("<onlyinclude>", "", StringComparison.Ordinal);
    }

    private static bool IsTemplate(string title)
    {
        return title.StartsWith("Template:", StringComparison.OrdinalIgnoreCase);
    }

    private static void ParseAndAssignRedirects(AcquisitionGraph graph, string title, Wikitext ast)
    {
        var target = DetectRedirect(ast);
        if (target != null)
            graph.CreateRedirect(title, target);
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
            if (itemName == null || cost == null || availability == null)
                continue;
            metadata.Add("cost", cost + " Gems");
            metadata.Add("availability", availability);
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
            "WEAPON SET" => NodeType.Set,
            "ARMOR SET" => NodeType.Set,

            "EVENT" => NodeType.Event,

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
    private void ApplyRelationships(
        AcquisitionGraph graph,
        string nodeId,
        Node node,
        Wikitext ast,
        InfoboxData info,
        CancellationToken cancellationToken)
    {
        // set
        var setData = info.Get("set");
        if (!string.IsNullOrWhiteSpace(setData))
        {
            graph.AddEdge(nodeId, setData, EdgeType.ContainedIn);
        }

        // Skin link
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

        // Location hierarchy
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


        // objects
        if (info.InfoBoxType.Equals("Object", StringComparison.OrdinalIgnoreCase))
        {
            HandleLocation(graph, nodeId, info);
        }

        // NPC that are merchants (Vendor)
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


                var texts = EnumerateWikiTexts(graph, nodeId, ast, cancellationToken);
                var vendorTableRows = texts.SelectMany(t => t.EnumDescendants().OfType<Template>().Where(t => t.Name.ToString().Contains("vendor table row", StringComparison.OrdinalIgnoreCase))).ToList();

                foreach (var row in vendorTableRows)
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

        if (info.InfoBoxType.Equals("NPC", StringComparison.OrdinalIgnoreCase) || 
            info.InfoBoxType.Equals("Event", StringComparison.OrdinalIgnoreCase)) { }
        {
            var rewardsItemTemplates = ast.EnumDescendants().OfType<Template>().Where(t => t.Name.ToString().Contains("Rewards item", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var rewardItemTemplate in rewardsItemTemplates)
            {
                var forbidden = new List<string>() { "profession" };
                var itemName = rewardItemTemplate.Arguments.FirstOrDefault()?.Value?.ToString();
                var anyContainsForbiddenArgument = rewardItemTemplate.Arguments.Any(a => a.Name != null && forbidden.Any(f => f.Contains(a.Name.ToString().Trim(), StringComparison.OrdinalIgnoreCase)));
                if (itemName != null && !anyContainsForbiddenArgument)
                {
                    graph.AddEdge(itemName, nodeId, EdgeType.RewardedBy);
                }
            }
        }

        // Container (metadata-driven)
        if (info.Get("type")?.Equals("Container", StringComparison.OrdinalIgnoreCase) == true || node.Type == NodeType.GemStoreCombo)
        {
            foreach (var template in ast.EnumDescendants().OfType<Template>())
            {
                var contains = template.Name.ToString().Equals("contains", StringComparison.OrdinalIgnoreCase);
                var containsSet = template.Name.ToString().Equals("contains set", StringComparison.OrdinalIgnoreCase);
                EdgeType? edgeType = null;
                if (contains || containsSet) {
                    edgeType = EdgeType.ContainedIn;
                }
                else
                {
                    continue;
                }

                var target = template.Arguments
                    .FirstOrDefault(a => a.Name == null || string.IsNullOrWhiteSpace(a.Name.ToString()))
                    ?.Value?.ToString()?.Trim();

                if (!string.IsNullOrWhiteSpace(target))
                {
                    graph.GetOrCreate(target);
                    graph.AddEdge(target, nodeId, edgeType.Value);
                }
            }
        }

        // Gathered from Object
        if (info.InfoBoxType.Equals("Object", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var template in ast.EnumDescendants().OfType<Template>())
            {
                var gather = template.Name.ToString().Equals("gather", StringComparison.OrdinalIgnoreCase);
                var containsSet = template.Name.ToString().Equals("contains set", StringComparison.OrdinalIgnoreCase);
                EdgeType? edgeType = null;
                if (gather)
                {
                    edgeType = EdgeType.GatheredFrom;
                }
                else if (containsSet)
                {
                    edgeType = EdgeType.ContainedIn;
                }
                else
                {
                    continue;
                }

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
                        edgeType.Value);
                }
            }
        }

        ApplyCrafting(graph, nodeId, ast);
    }

    private IEnumerable<Wikitext> EnumerateWikiTexts(
        AcquisitionGraph graph,
        string rootId,
        Wikitext rootWikiText,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var stack = new Stack<(string Id, Wikitext WikiText)>();

        stack.Push((rootId, rootWikiText));

        static bool startsWithTemplateKey(Template template, string key)
        {
            var argsWithoutValue = template.Arguments
                .Where(a => a.Name == null)
                .Select(a => a.Value);

            var suffix = string.Join('|', argsWithoutValue);

            return key.Equals(
                template.Name.ToString().Trim() + "|" + suffix,
                StringComparison.OrdinalIgnoreCase);
        }

        while (stack.TryPop(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!visited.Add(item.Id))
            {
                continue;
            }

            yield return item.WikiText;

            var templatesOnPage = item.WikiText
                .EnumDescendants()
                .OfType<Template>()
                .ToList();

            // Traverse subpages.
            if (graph.SubPages.TryGetValue(item.Id, out var subPageContents))
            {
                for (var i = 0; i < subPageContents.Count; i++)
                {
                    stack.Push((
                        $"{item.Id}/subpage/{i}",
                        parser.Parse(subPageContents[i], cancellationToken)));
                }
            }

            // Traverse templates.
            foreach (var template in graph.Templates)
            {
                if (!templatesOnPage.Any(t => startsWithTemplateKey(t, template.Key)))
                {
                    continue;
                }

                stack.Push((
                    $"template:{template.Key}",
                    parser.Parse(template.Value, cancellationToken)));
            }
        }
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

                // skip historical
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