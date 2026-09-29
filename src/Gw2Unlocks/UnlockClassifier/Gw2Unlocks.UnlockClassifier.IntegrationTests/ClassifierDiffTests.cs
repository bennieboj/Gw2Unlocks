using Gw2Unlocks.UnlockClassifier;
using Gw2Unlocks.UnlockClassifier.Implementation;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Gw2Unlocks.UnlockClassifier.IntegrationTests;

/// <summary>
/// Unit tests for the diff between two classifications: what counts as added, moved and removed,
/// and what that means for the pipeline gate. These build configs in memory, so they need no
/// cached data.
/// </summary>
public class ClassifierDiffTests
{
    private static UnlockCategory Node(string name, string[] unlocks, params UnlockCategory[] subCategories) =>
        new()
        {
            Name = name,
            Unlocks = [.. unlocks.Select(u => new Unlock(u, null!))],
            SubCategories = [.. subCategories]
        };

    private static ClassifyConfig Config(params UnlockCategory[] categories) =>
        new() { Categories = [.. categories] };

    [Fact]
    public void GivenAnUnchangedConfigWhenDiffingThenNothingIsReported()
    {
        var config = Config(Node("A", ["One", "Two"]));

        var diff = ClassifierDiff.Build(config, config);

        Assert.Equal(0, diff.Added);
        Assert.Equal(0, diff.Moved);
        Assert.Equal(0, diff.Removed);
        Assert.False(diff.HasRegressions);
    }

    [Fact]
    public void GivenANewUnlockWhenDiffingThenItIsAnAdditionAndNotARegression()
    {
        var oldConfig = Config(Node("A", ["One"]));
        var newConfig = Config(Node("A", ["One", "Two"]));

        var diff = ClassifierDiff.Build(oldConfig, newConfig);

        Assert.Equal(1, diff.Added);
        Assert.False(diff.HasRegressions);
    }

    [Fact]
    public void GivenADroppedUnlockWhenDiffingThenItIsARemovalAndAReduction()
    {
        var oldConfig = Config(Node("A", ["One", "Two"]));
        var newConfig = Config(Node("A", ["One"]));

        var diff = ClassifierDiff.Build(oldConfig, newConfig);

        Assert.Equal(1, diff.Removed);
        Assert.True(diff.HasRegressions);
    }

    /// <summary>
    /// A moved unlock belongs to two nodes: the one it left and the one it arrived at. The node it
    /// left would otherwise report it as removed, so an ordinary reshuffle would look like twice
    /// as much damage as it is.
    /// </summary>
    [Fact]
    public void GivenAnUnlockMovedBetweenNodesWhenDiffingThenItCountsAsOneMoveAndNotARemoval()
    {
        var oldConfig = Config(Node("A", ["Shared"], Node("B", ["Moved"])));
        var newConfig = Config(Node("A", ["Shared", "Moved"], Node("B", [])));

        var diff = ClassifierDiff.Build(oldConfig, newConfig);

        Assert.Equal(1, diff.Moved);
        Assert.Equal(0, diff.Removed);
        Assert.True(diff.HasRegressions);
    }

    [Fact]
    public void GivenANewCategoryPathWhenDiffingThenItsUnlocksAreAdditions()
    {
        var oldConfig = Config(Node("A", ["One"]));
        var newConfig = Config(Node("A", ["One"], Node("B", [], Node("C", ["Two"]))));

        var diff = ClassifierDiff.Build(oldConfig, newConfig);

        Assert.Equal(1, diff.Added);
        Assert.Equal(0, diff.Moved);
        Assert.Equal(0, diff.Removed);
        Assert.False(diff.HasRegressions);
    }

    /// <summary>
    /// A node that only lost unlocks to another node has nothing to report, so it must not appear
    /// as an empty bullet in the rendered diff.
    /// </summary>
    [Fact]
    public void GivenANodeThatOnlyLostUnlocksToAnotherNodeWhenRenderingThenItIsLeftOut()
    {
        var oldConfig = Config(Node("A", ["Moved"], Node("B", [])));
        var newConfig = Config(Node("A", [], Node("B", ["Moved"])));

        var markdown = ClassifierDiff.Build(oldConfig, newConfig).ToMarkdown();

        Assert.Contains("- [*] `Moved` (from A)", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("- A\n", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenAMoveWhenRenderingThenItIsANestedListUnderItsNewCategory()
    {
        var oldConfig = Config(Node("Root", [], Node("Child", ["Moved"]), Node("Sibling", [])));
        var newConfig = Config(Node("Root", [], Node("Child", []), Node("Sibling", ["Moved"])));

        var diff = ClassifierDiff.Build(oldConfig, newConfig);
        var markdown = diff.ToMarkdown();

        Assert.Equal(1, diff.Moved);
        Assert.Contains("Listing all diffs:", markdown, StringComparison.Ordinal);
        Assert.Contains("Diff summary: 0 added, 1 moved, 0 removed", markdown, StringComparison.Ordinal);
        Assert.Contains("  - Root / Sibling", markdown, StringComparison.Ordinal);
        Assert.Contains("    - [*] `Moved` (from Root / Child)", markdown, StringComparison.Ordinal);
    }

    /// <summary>
    /// An unlock that lands on a category path which did not exist before is reported as an
    /// addition, because that path is new and everything on it is new to it. Pinned here so the
    /// promise that additions never block a run is deliberate rather than incidental.
    /// </summary>
    [Fact]
    public void GivenAnUnlockMovingOntoANewCategoryPathWhenDiffingThenItIsAnAddition()
    {
        var oldConfig = Config(Node("Root", [], Node("Child", ["Moved"])));
        var newConfig = Config(Node("Root", [], Node("Child", [], Node("Grandchild", ["Moved"]))));

        var diff = ClassifierDiff.Build(oldConfig, newConfig);

        Assert.Equal(1, diff.Added);
        Assert.Equal(0, diff.Moved);
        Assert.Equal(0, diff.Removed);
        Assert.False(diff.HasRegressions);
    }
}
