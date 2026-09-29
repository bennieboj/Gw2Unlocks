using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Gw2Unlocks.UnlockClassifier.Implementation;

/// <summary>
/// The difference between the classification that was cached and the one that has just been
/// computed, together with the single rendering of it. Pure: no logging, no I/O and no
/// environment, so the rules that decide whether a run regressed can be asserted on directly.
/// </summary>
/// <remarks>
/// The rendering is markdown, but it is written so that it also reads well as plain text in a
/// terminal: two spaces of indent per level is both the shape the log wants and the indent markdown
/// needs for a nested list, so one string serves both the run log and the pull request body.
/// </remarks>
internal sealed class ClassifierDiff
{
    private const char PathSeparator = '\u001f';
    private const string DisplayPathSeparator = " / ";

    private readonly List<string> _lines = [];

    private ClassifierDiff()
    {
    }

    /// <summary>Unlocks the new config has and the old one did not.</summary>
    public int Added { get; private set; }

    /// <summary>Unlocks the new config classifies under a different category than before.</summary>
    public int Moved { get; private set; }

    /// <summary>Unlocks the new config no longer classifies at all.</summary>
    public int Removed { get; private set; }

    /// <summary>
    /// True when the new classification dropped or relocated an unlock. Additions are expected;
    /// losses and moves are the ones a human has to look at.
    /// </summary>
    public bool HasRegressions => Moved > 0 || Removed > 0;

    public static ClassifierDiff Build(ClassifyConfig oldConfig, ClassifyConfig newConfig)
    {
        ArgumentNullException.ThrowIfNull(oldConfig);
        ArgumentNullException.ThrowIfNull(newConfig);

        var diff = new ClassifierDiff();
        var oldLocations = BuildLocationMap(oldConfig);
        var newLocations = BuildLocationMap(newConfig);

        var oldNodes = oldConfig.GetNodes().ToDictionary(x => x.Path.Key, x => x.Node);
        var newNodes = newConfig.GetNodes().ToDictionary(x => x.Path.Key, x => x.Node);

        // Walk the new config in its own order so the diff reads in the same order as the site,
        // then append nodes that only the old config has.
        var keys = newConfig.GetNodes()
            .Select(x => x.Path.Key)
            .Concat(oldConfig.GetNodes()
                .Select(x => x.Path.Key)
                .Where(k => !newNodes.ContainsKey(k)));

        foreach (var key in keys)
        {
            oldNodes.TryGetValue(key, out var oldNode);
            newNodes.TryGetValue(key, out var newNode);

            // Indent by depth so the output mirrors the shape of the tree.
            var depth = key.Split(PathSeparator).Length;

            if (oldNode is null && newNode is not null)
            {
                diff.WriteNode(depth, FormatPath(key), indent =>
                    diff.PrintAdds(newNode.Unlocks, indent));
                continue;
            }

            if (oldNode is not null && newNode is null)
            {
                diff.WriteNode(depth, FormatPath(key), indent =>
                    diff.PrintRemoves(oldNode.Unlocks, indent, oldLocations, newLocations));
                continue;
            }

            if (oldNode is null || newNode is null || !HasUnlockChanges(oldNode.Unlocks, newNode.Unlocks))
                continue;

            diff.WriteNode(depth, FormatPath(key), indent =>
                diff.PrintUnlockDiff(oldNode.Unlocks, newNode.Unlocks, indent, oldLocations, newLocations));
        }

        return diff;
    }

    /// <summary>
    /// The whole diff as markdown: the heading, the counts, and the nested list of changes.
    /// </summary>
    public string ToMarkdown()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Listing all diffs:");
        builder.AppendLine();
        builder.Append("Diff summary: ").Append(Added).Append(" added, ")
               .Append(Moved).Append(" moved, ").Append(Removed).AppendLine(" removed");
        builder.AppendLine();

        foreach (var line in _lines)
            builder.AppendLine(line);

        return builder.ToString();
    }

    /// <summary>
    /// Writes a category heading followed by whatever its children have to say, and nothing at all
    /// when they have nothing to say: a node that only lost unlocks to another node would otherwise
    /// be rendered as an empty bullet.
    /// </summary>
    private void WriteNode(int depth, string label, Action<int> writeChildren)
    {
        var firstChild = _lines.Count;

        writeChildren(depth + 1);

        if (_lines.Count > firstChild)
            _lines.Insert(firstChild, Indent(depth, $"- {label}"));
    }

    private void PrintUnlockDiff(
        Collection<Unlock> oldUnlocks,
        Collection<Unlock> newUnlocks,
        int indent,
        Dictionary<string, string> oldLocations,
        Dictionary<string, string> newLocations)
    {
        var oldNames = oldUnlocks.Select(x => x.Name).ToHashSet();
        var newNames = newUnlocks.Select(x => x.Name).ToHashSet();

        foreach (var name in newNames.Except(oldNames).OrderBy(x => x, StringComparer.Ordinal))
        {
            if (oldLocations.TryGetValue(name, out var previousLocation))
            {
                Moved++;
                Write(indent, $"- [*] `{name}` (from {previousLocation})");
            }
            else
            {
                Added++;
                Write(indent, $"- [+] `{name}`");
            }
        }

        foreach (var name in oldNames.Except(newNames).OrderBy(x => x, StringComparer.Ordinal))
        {
            // An unlock that turned up somewhere else is reported once, as a move at its new
            // location. Only an unlock missing from the whole new config is a removal.
            if (newLocations.ContainsKey(name))
                continue;

            Removed++;

            if (oldLocations.TryGetValue(name, out var previousLocation))
                Write(indent, $"- [-] `{name}` (from {previousLocation})");
        }
    }

    private void PrintAdds(Collection<Unlock> unlocks, int indent)
    {
        // A category path that did not exist before: its unlocks are new to this path, so they are
        // counted as additions even when the same unlock used to sit on a different path. The
        // pipeline treats additions as expected, so a move into a brand new category is not
        // reported as a regression.
        foreach (var unlock in unlocks.OrderBy(x => x.Name))
        {
            Added++;
            Write(indent, $"- [+] `{unlock.Name}`");
        }
    }

    private void PrintRemoves(
        Collection<Unlock> unlocks,
        int indent,
        Dictionary<string, string> oldLocations,
        Dictionary<string, string> newLocations)
    {
        foreach (var unlock in unlocks.OrderBy(x => x.Name))
        {
            if (newLocations.ContainsKey(unlock.Name))
                continue;

            Removed++;

            if (oldLocations.TryGetValue(unlock.Name, out var previousLocation))
                Write(indent, $"- [-] `{unlock.Name}` (from {previousLocation})");
        }
    }

    private static Dictionary<string, string> BuildLocationMap(ClassifyConfig config)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (node, path) in config.GetNodes())
        {
            foreach (var unlock in node.Unlocks)
                map[unlock.Name] = FormatPath(path.Key);
        }

        return map;
    }

    private static bool HasUnlockChanges(
        Collection<Unlock> oldUnlocks,
        Collection<Unlock> newUnlocks)
    {
        var oldNames = oldUnlocks.Select(x => x.Name).ToHashSet();
        var newNames = newUnlocks.Select(x => x.Name).ToHashSet();

        return !oldNames.SetEquals(newNames);
    }

    private static string FormatPath(string key) =>
        key.Replace(PathSeparator.ToString(), DisplayPathSeparator, StringComparison.Ordinal);

    private void Write(int indent, string text) => _lines.Add(Indent(indent, text));

    private static string Indent(int indent, string text) => $"{new string(' ', indent * 2)}{text}";
}
