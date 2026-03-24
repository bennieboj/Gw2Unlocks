using Gw2Unlocks.Wiki.WikiApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.Implementation;

public record ItemAcquisitionNode(string Title) : AcquisitionNode(Title);
public record VendorAcquisitionNode(string Title) : AcquisitionNode(Title);
public record AchievementAcquisitionNode(string Title) : AcquisitionNode(Title);
public record ContainerAcquisitionNode(string Title) : AcquisitionNode(Title);
public record SkinAcquisitionNode(string Title) : AcquisitionNode(Title);

public record AreaAcquisitionNode(string Title) : AcquisitionNode(Title);
public record ZoneAcquisitionNode(string Title) : AcquisitionNode(Title);


public partial class Gw2WikiSource(IWikiApi api) : IGw2WikiSource
{
    public async Task<ReadOnlyCollection<UnlockInfo>> GetAllUnlocks(ICollection<string> pageTitles, CancellationToken cancellationToken)
    {
        var allTitles = pageTitles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        //allTitles = new List<string> {
        //                    "Mini Exalted Sage",
        //                    "Endless Exalted Caster Tonic",
        //                    "Luminate's Backplate (skin)"
        //};

        var result = new List<UnlockInfo>();

        foreach (var title in allTitles)
        {
            var root = await ResolveInternal(title, new HashSet<string>(), cancellationToken);

            if (root == null)
                continue;

            var paths = GetPathsToTerminal(root);

            if (paths.Count == 0)
                continue;

            result.Add(new UnlockInfo(title, root));
        }

        return new ReadOnlyCollection<UnlockInfo>(result);
    }

    // ---------------- GRAPH TRAVERSAL ----------------

    public static IReadOnlyList<IReadOnlyList<AcquisitionNode>> GetPathsToTerminal(AcquisitionNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var results = new List<List<AcquisitionNode>>();
        var current = new List<AcquisitionNode>();

        static bool IsTerminal(AcquisitionNode node) =>
            node is ZoneAcquisitionNode or AchievementAcquisitionNode;

        void Traverse(AcquisitionNode node)
        {
            current.Add(node);

            if (IsTerminal(node))
            {
                results.Add(new List<AcquisitionNode>(current));
            }
            else
            {
                foreach (var next in node.Next)
                    Traverse(next);
            }

            current.RemoveAt(current.Count - 1);
        }

        Traverse(root);

        return results
            .Select(path => (IReadOnlyList<AcquisitionNode>)path)
            .ToList();
    }

    // ---------------- RESOLUTION ----------------

    private async Task<AcquisitionNode?> ResolveInternal(
        string title,
        HashSet<string> visited,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(title))
            return null;

        var node = new ItemAcquisitionNode(title);

        // SOLD BY
        var soldByText = await ExpandAsync($"{{{{Sold by|{title}}}}}", cancellationToken);

        if (!string.IsNullOrWhiteSpace(soldByText) /*&& !soldByText.Contains("No results for sold by", StringComparison.InvariantCulture)*/)
        {
            var rows = ParseSoldByTable(soldByText);

            foreach (var (vendor, areas, zones) in rows)
            {
                var vendorNode = new VendorAcquisitionNode(vendor);

                var count = Math.Min(areas.Count, zones.Count);

                for (int i = 0; i < count; i++)
                {
                    var areaNode = new AreaAcquisitionNode(areas[i]);
                    var zoneNode = new ZoneAcquisitionNode(zones[i]);

                    areaNode.AddNext(zoneNode);
                    vendorNode.AddNext(areaNode);
                }

                node.AddNext(vendorNode);

                // attach cost to unlock (or collect it)
                //unlockCost = cost; // depends on your flow
            }

            if (rows.Count > 0)
                return node;
        }

        // ACHIEVEMENT
        var content = await GetPageWikitextAsync(title, cancellationToken);
        var achievement = ExtractAchievement(content);

        if (!string.IsNullOrEmpty(achievement))
        {
            var achievementNode = new AchievementAcquisitionNode(achievement);
            node.AddNext(achievementNode);

            // STOP traversal here (per your test)
            return node;
        }

        // CONTAINERS
        var containers = await GetLinksFromTemplate($"{{{{contained in|{title}}}}}", cancellationToken);

        foreach (var container in containers)
        {
            var containerNode = new ContainerAcquisitionNode(container);
            node.AddNext(containerNode);

            var sub = await ResolveInternal(container, visited, cancellationToken);

            if (sub != null)
            {
                foreach (var next in sub.Next)
                    containerNode.AddNext(next);
            }
        }

        // SKIN
        if (title.Contains("(skin)", StringComparison.OrdinalIgnoreCase))
        {
            var items = await GetLinksFromTemplate($"{{{{skin list|{title}}}}}", cancellationToken);

            foreach (var item in items)
            {
                var sub = await ResolveInternal(item, visited, cancellationToken);
                if (sub != null)
                    node.AddNext(sub);
            }
        }

        return node.Next.Count > 0 ? node : null;
    }

    // ---------------- LOCATION ----------------

    private static List<(string vendor, List<string> areas, List<string> zones)> ParseSoldByTable(string text)
    {
        var result = new List<(string, List<string>, List<string>)>();

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
        List<(string vendor, List<string> areas, List<string> zones)> result)
    {
        // Expect at least 4 columns: Vendor, Area, Zone, Cost(, Notes)
        if (rowLines.Count < 3)
            return;

        // Clean cell content: remove leading '|'
        var cells = rowLines
            .Select(l => l.Length > 1 ? l.Substring(1).Trim() : "")
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
        //var cost = CleanCost(cells[3]);

        if (areas.Count == 0 || zones.Count == 0)
            return;

        result.Add((vendor, areas, zones));
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
        links = links.Where(l => !"Category:Pages with empty semantic mediawiki query results".Equals(l, StringComparison.OrdinalIgnoreCase)).ToList() ;
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
        return LinkRegex()
            .Matches(text)
            .Select(m => m.Groups[1].Value.Split('|')[0].Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("File:", StringComparison.OrdinalIgnoreCase)) // filter out files
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("SMW::", StringComparison.OrdinalIgnoreCase)) // filter out SMW
            .Where(x => !string.IsNullOrWhiteSpace(x)) // FIX (CS8619)
            .Distinct()
            .ToList();
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
