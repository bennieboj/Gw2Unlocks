using Gw2Unlocks.Wiki.WikiApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gw2Unlocks.Wiki.Implementation;

public sealed partial class Gw2WikiSource(ILogger<Gw2WikiSource> logger, IWikiApi api) : IGw2WikiSource
{
    public async Task<AcquisitionGraph> GetAcquisitionGraph(IEnumerable<string> itemNames, AcquisitionGraph? existingGraph = null, CancellationToken cancellationToken = default)
    {
        var allTitles = itemNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var graph = existingGraph ?? new AcquisitionGraph();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var title in allTitles)
        {
            var sw = Stopwatch.StartNew();
            await BuildGraph(graph, title, visited, CancellationToken.None);
            sw.Stop();
            logger.LogInformation("Processing page in {ms}: {Title}", sw.ElapsedMilliseconds, title);

            //bail if cancellation requested after each page to avoid long waits
            cancellationToken.ThrowIfCancellationRequested();
        }

        return graph;
    }

    // ---------------- GRAPH TRAVERSAL ----------------

    private async Task BuildGraph(
     AcquisitionGraph graph,
     string title,
     HashSet<string> visited,
     CancellationToken cancellationToken,
     NodeType nodeType = NodeType.Item)
    {
        if (!visited.Add(title))
            return;

        var itemNode = graph.GetOrCreate(nodeType, title);

        if (itemNode.IsProcessed)
            return;

        // --- SOLD BY ---
        var soldByText = await ExpandAsync($"{{{{Sold by|{title}}}}}", cancellationToken);

        if (!string.IsNullOrWhiteSpace(soldByText) &&
            !soldByText.Contains("No results for sold by", StringComparison.InvariantCulture))
        {
            var rows = ParseSoldByTable(soldByText);

            foreach (var (vendor, areas, zones, cost) in rows)
            {
                var vendorNode = graph.GetOrCreate(NodeType.Vendor, vendor);

                var count = Math.Min(areas.Count, zones.Count);
                for (int i = 0; i < count; i++)
                {
                    var areaNode = graph.GetOrCreate(NodeType.Area, areas[i]);
                    areaNode.IsProcessed = true;
                    var zoneNode = graph.GetOrCreate(NodeType.Zone, zones[i]);
                    areaNode.IsProcessed = true;

                    graph.AddEdge(vendorNode.Id, areaNode.Id, EdgeType.LocatedIn);
                    graph.AddEdge(areaNode.Id, zoneNode.Id, EdgeType.LocatedIn);
                }

                if (rows.Count > 0)
                {
                    graph.AddEdge(itemNode.Id, vendorNode.Id, EdgeType.SoldBy,
                        new Dictionary<string, string> { ["cost"] = cost });
                    vendorNode.IsProcessed = true;
                }
            }
        }

        // --- ACHIEVEMENT ---
        var content = await GetPageWikitextAsync(title, cancellationToken);
        var achievement = ExtractAchievement(content);

        if (!string.IsNullOrEmpty(achievement))
        {
            var achievementNode = graph.GetOrCreate(NodeType.Achievement, achievement);
            graph.AddEdge(itemNode.Id, achievementNode.Id, EdgeType.Rewards);
            achievementNode.IsProcessed = true;
        }

        // --- CONTAINERS ---
        var containers = await GetLinksFromTemplate($"{{{{contained in|{title}}}}}", cancellationToken);
        foreach (var container in containers)
        {
            var containerNode = graph.GetOrCreate(NodeType.Container, container);
            graph.AddEdge(itemNode.Id, containerNode.Id, EdgeType.Contains);

            await BuildGraph(graph, container, visited, cancellationToken, NodeType.Container);
        }

        // --- SKINS ---
        if (title.Contains("(skin)", StringComparison.OrdinalIgnoreCase))
        {
            var items = await GetLinksFromTemplate($"{{{{skin list|{title}}}}}", cancellationToken);

            foreach (var item in items)
            {
                var itemNode2 = graph.GetOrCreate(NodeType.Item, item);
                graph.AddEdge(itemNode.Id, itemNode2.Id, EdgeType.SkinUnlock);

                await BuildGraph(graph, item, visited, cancellationToken);
            }
        }


        itemNode.IsProcessed = true;
    }


    // ---------------- LOCATION ----------------

    private static List<(string vendor, List<string> areas, List<string> zones, string cost)> ParseSoldByTable(string text)
    {
        var result = new List<(string, List<string>, List<string>, string)>();

        if (string.IsNullOrWhiteSpace(text))
            return result;

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var currentRow = new List<string>();
        var inRow = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.StartsWith("|-", StringComparison.Ordinal))
            {
                // Finish previous row
                if (currentRow.Count > 0)
                    ProcessRow(currentRow, result);

                currentRow.Clear();
                inRow = true;
                continue;
            }

            if (line.StartsWith("|}", StringComparison.Ordinal))
            {
                if (currentRow.Count > 0)
                    ProcessRow(currentRow, result);

                break;
            }

            if (inRow && line.StartsWith('|'))
            {
                currentRow.Add(line);
            }
        }

        return result;
    }

    private static void ProcessRow(
        List<string> rowLines,
        List<(string vendor, List<string> areas, List<string> zones, string cost)> result)
    {
        // Expect at least 4 columns: Vendor, Area, Zone, Cost(, Notes)
        if (rowLines.Count < 3)
            return;

        // Clean cell content: remove leading '|'
        var cells = rowLines
            .Select(l => l.Length > 1 ? l[1..].Trim() : "")
            .ToList();

        // --- Vendor ---
        var vendorLinks = ExtractLinks(cells[0]);
        if (vendorLinks.Count == 0)
            return;

        var vendor = vendorLinks[0].Split('#')[0];

        // --- Areas ---
        var areas = ExtractLinks(cells[1]);

        // --- Zones ---
        var zones = ExtractLinks(cells[2])
            .Select(z => z.TrimStart(':'))
            .ToList();

        // --- Cost (raw text, cleaned a bit) ---
        var cost = CleanCost(cells[3]);

        if (areas.Count == 0 || zones.Count == 0)
            return;

        result.Add((vendor, areas, zones, cost));
    }

    private static string CleanCost(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove style prefix: "style="..." |"
        var pipeIndex = text.IndexOf('|', StringComparison.Ordinal);
        if (pipeIndex >= 0)
            text = text[(pipeIndex + 1)..];

        return text
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("<nowiki/>", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    // ---------------- HELPERS ----------------
    private static string? ExtractAchievement(string wikitext)
    {
        if (string.IsNullOrEmpty(wikitext))
            return null;

        var match = AchievementBoxRegex().Match(wikitext);

        if (!match.Success)
            return null;

        return match.Groups[1].Value
        .Split('|', StringSplitOptions.RemoveEmptyEntries)[0]
        .Trim();
    }

    private async Task<List<string>> GetLinksFromTemplate(string template, CancellationToken cancellationToken)
    {
        var text = await ExpandAsync(template, cancellationToken);
        var links = ExtractLinks(text);
        links = [.. links.Where(l => !"Category:Pages with empty semantic mediawiki query results".Equals(l, StringComparison.OrdinalIgnoreCase))];
        return links;
    }

    private async Task<string> GetPageWikitextAsync(string title, CancellationToken cancellationToken)
    {
        var result = await api.QueryAsync(new
        {
            action = "query",
            prop = "revisions",
            titles = title,
            rvprop = "content",
            format = "json"
        }, cancellationToken);

        var pages = result?["query"]?["pages"]?.AsObject();

        if (pages == null || pages.Count == 0)
            return "";

        var page = pages.First().Value;

        return page?["revisions"]?[0]?["slots"]?["main"]?["*"]?.GetValue<string>()
            ?? page?["revisions"]?[0]?["*"]?.GetValue<string>()
            ?? "";
    }

    private async Task<string> ExpandAsync(string template, CancellationToken cancellationToken)
    {
        var result = await api.QueryAsync(new
        {
            action = "expandtemplates",
            text = template,
            format = "json",
            prop = "wikitext"
        }, cancellationToken);

        var raw = result?["expandtemplates"]?["wikitext"]?.GetValue<string>() ?? "";

        return CleanWikitext(raw);
    }

    private static string CleanWikitext(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text
            // Remove SMW markers
            .Replace("[[SMW::off]]", "", StringComparison.OrdinalIgnoreCase)
            .Replace("[[SMW::on]]", "", StringComparison.OrdinalIgnoreCase)

            // Normalize line endings
            .Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("\r", "\n", StringComparison.OrdinalIgnoreCase)

            // Decode common HTML entities (minimal set)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)

            // Trim overall
            .Trim();
    }


    private static List<string> ExtractLinks(string text)
    {
        return [.. LinkRegex()
            .Matches(text)
            .Select(m => m.Groups[1].Value.Split('|')[0].Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("File:", StringComparison.OrdinalIgnoreCase)) // filter out files
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("SMW::", StringComparison.OrdinalIgnoreCase)) // filter out SMW
            .Where(x => !string.IsNullOrWhiteSpace(x)) // FIX (CS8619)
            .Distinct()];
    }

    private static string? ExtractLocation(string content)
    {
        var match = LocationRegex().Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static (string?, string?) ParseLocationInfo(string content)
    {
        var type = TypeRegex().Match(content);
        var within = WithinRegex().Match(content);

        return (
            type.Success ? type.Groups[1].Value.Trim() : null,
            within.Success ? within.Groups[1].Value.Trim() : null
        );
    }

    // ---------------- REGEX (SYSLIB1045 FIX) ----------------

    [GeneratedRegex(@"\|\s*location\s*=\s*(.+)", RegexOptions.Compiled)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"\|\s*type\s*=\s*([^\}\r\n]+)", RegexOptions.Compiled)]
    private static partial Regex TypeRegex();

    [GeneratedRegex(@"\|\s*within\s*=\s*([^\}\r\n]+)", RegexOptions.Compiled)]
    private static partial Regex WithinRegex();

    [GeneratedRegex(@"\[\[(.*?)\]\]", RegexOptions.Compiled)]
    public static partial Regex LinkRegex();

    [GeneratedRegex(@"\{\{\s*achievement box\s*\|\s*(.*?)\s*\}\}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    public static partial Regex AchievementBoxRegex();
}
