using Gw2Unlocks.UnlockClassifier;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Gw2Unlocks.UnlockClassifier.IntegrationTests;

/// <summary>
/// Unit tests for the classification tree itself: path identity, traversal and rollups. These use
/// only the public contract API and need no cached data.
/// </summary>
public class ClassifyConfigTreeTests
{
    private static UnlockCategory Node(string name, params UnlockCategory[] subCategories) =>
        new() { Name = name, SubCategories = [.. subCategories] };

    private static ClassifyConfig Config() =>
        new()
        {
            Categories =
            [
                Node("A", Node("B", Node("C"))),
                Node("D"),
            ]
        };

    [Fact]
    public void GivenIndependentlyBuiltPathsToTheSameChainWhenComparingThenPathsAreEqual()
    {
        var a = new UnlockCategory { Name = "A" };
        var b = new UnlockCategory { Name = "B" };
        var c = new UnlockCategory { Name = "C" };

        var path1 = new CategoryPath(ImmutableArray.Create(a, b, c));
        var path2 = new CategoryPath(ImmutableArray.Create(a, b, c));

        // ImmutableArray<T>.Equals compares the array reference, not the elements, so this
        // equality has to be implemented by CategoryPath itself.
        Assert.Equal(path1, path2);
        Assert.True(path1 == path2);
        Assert.Equal(path1.GetHashCode(), path2.GetHashCode());
    }

    [Fact]
    public void GivenPathsOfDifferentDepthWhenComparingThenPathsAreNotEqual()
    {
        var a = new UnlockCategory { Name = "A" };
        var b = new UnlockCategory { Name = "B" };

        var shallow = new CategoryPath(ImmutableArray.Create(a));
        var deep = new CategoryPath(ImmutableArray.Create(a, b));

        Assert.NotEqual(shallow, deep);
        Assert.True(shallow != deep);
    }

    [Fact]
    public void GivenEquivalentPathsWhenGroupingThenEquivalentPathsCountTogether()
    {
        var a = new UnlockCategory { Name = "A" };
        var b = new UnlockCategory { Name = "B" };

        var first = new CategoryPath(ImmutableArray.Create(a, b));
        var second = new CategoryPath(ImmutableArray.Create(a, b));
        var other = new CategoryPath(ImmutableArray.Create(a));

        var groups = new[] { first, second, other }.GroupBy(x => x).ToList();

        // The classifier picks the winning classification by group size; if equivalent paths did
        // not group together this would silently pick the first candidate instead.
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups.Single(g => g.Key == first).Count());
        Assert.Single(groups.Single(g => g.Key == other));
    }

    [Fact]
    public void GivenAPathWhenReadingItThenDepthAndLeafAreExposed()
    {
        var a = new UnlockCategory { Name = "A" };
        var b = new UnlockCategory { Name = "B" };
        var c = new UnlockCategory { Name = "C" };

        var root = new CategoryPath(ImmutableArray.Create(a));
        var deep = new CategoryPath(ImmutableArray.Create(a, b, c));

        Assert.True(root.IsRoot);
        Assert.Equal(3, deep.Depth);
        Assert.False(deep.IsRoot);
        Assert.Same(c, deep.Leaf);
        Assert.Equal("A/B/C", deep.ToString());
    }

    [Fact]
    public void GivenAThreeLevelTreeWhenWalkingItThenEveryNodeIsVisitedWithItsPath()
    {
        var paths = Config().GetPathedCategories()
            .Select(x => string.Join("/", x.Path))
            .ToList();

        Assert.Equal(["A", "A/B", "A/B/C", "D"], paths);
    }

    [Fact]
    public void GivenANodeWithDescendantsWhenRollingUpUnlocksThenDescendantsAreIncludedOnce()
    {
        var c = new UnlockCategory { Name = "C" };
        c.Unlocks.Add(new Unlock("deep", null!));
        var b = new UnlockCategory { Name = "B", SubCategories = [c] };
        b.Unlocks.Add(new Unlock("mid", null!));
        var a = new UnlockCategory { Name = "A", SubCategories = [b] };
        a.Unlocks.Add(new Unlock("top", null!));

        var names = a.GetUnlocksWithDescendants().Select(u => u.Name).ToList();

        Assert.Equal(["top", "mid", "deep"], names);

        // Each unlock is stored once in the tree, so the rollup must not repeat any of them.
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void GivenALeafWhenRollingUpUnlocksThenOnlyItsOwnUnlocksAreReturned()
    {
        var c = new UnlockCategory { Name = "C" };
        c.Unlocks.Add(new Unlock("only", null!));

        Assert.Equal("only", Assert.Single(c.GetUnlocksWithDescendants()).Name);
    }

    [Fact]
    public void GivenOnlyADescendantHasUnlocksWhenCheckingForUnlocksThenAncestorsReportTrue()
    {
        var c = new UnlockCategory { Name = "C" };
        c.Unlocks.Add(new Unlock("deep", null!));
        var b = new UnlockCategory { Name = "B", SubCategories = [c] };
        var a = new UnlockCategory { Name = "A", SubCategories = [b] };

        Assert.True(a.HasAnyUnlocks());
        Assert.True(b.HasAnyUnlocks());
        Assert.False(new UnlockCategory { Name = "Empty" }.HasAnyUnlocks());
    }

    [Fact]
    public void GivenAThreeLevelTreeWhenGettingAllCategoriesThenEveryNodeIsReturned()
    {
        Assert.Equal(4, Config().GetAllCategories().Count());
    }
}
