using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gw2Unlocks.WikiProcessing;

namespace Gw2Unlocks.CacheInspector.Host;

internal static class GraphTraversal
{
    public static IEnumerable<string> Walk(AcquisitionGraph graph, string start, bool incoming, int maxDepth, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(start);
        ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
        var edges = graph.Edges.ToLookup(e => incoming ? e.To : e.From, StringComparer.Ordinal);
        var expanded = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<(string Key, int Depth, string Label, HashSet<string> Path)>();
        stack.Push((start, 0, start, new HashSet<string>(StringComparer.Ordinal)));
        while (stack.TryPop(out var entry))
        {
            token.ThrowIfCancellationRequested();
            var prefix = new string(' ', checked(entry.Depth * 2));
            if (entry.Path.Contains(entry.Key))
            {
                yield return prefix + entry.Label + " [cycle]";
                continue;
            }
            if (expanded.TryGetValue(entry.Key, out var previousDepth) && previousDepth <= entry.Depth)
            {
                yield return prefix + entry.Label + " [already expanded]";
                continue;
            }
            var links = edges[entry.Key].OrderBy(e => incoming ? e.From : e.To, StringComparer.Ordinal)
                .ThenBy(e => e.Type).ToArray();
            if (entry.Depth == maxDepth)
            {
                yield return prefix + entry.Label + (links.Length > 0 ? " [depth limit]" : "");
                continue;
            }
            yield return prefix + entry.Label;
            expanded[entry.Key] = entry.Depth;
            var path = new HashSet<string>(entry.Path, StringComparer.Ordinal) { entry.Key };
            foreach (var edge in links.Reverse())
            {
                var target = incoming ? edge.From : edge.To;
                var metadata = edge.Metadata is { Count: > 0 }
                    ? " (" + string.Join(", ", edge.Metadata.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}")) + ")"
                    : "";
                stack.Push((target, entry.Depth + 1, $"{edge.Type} -> {target}{metadata}", path));
            }
        }
    }
}
