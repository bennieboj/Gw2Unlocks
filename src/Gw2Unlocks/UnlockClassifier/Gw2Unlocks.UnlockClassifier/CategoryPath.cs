using System;
using System.Collections.Immutable;
using System.Linq;

namespace Gw2Unlocks.UnlockClassifier;

/// <summary>
/// The location of a single node in the <see cref="ClassifyConfig"/> tree, as the chain of
/// categories from the root down to (and including) that node.
/// </summary>
/// <remarks>
/// Equality is by <see cref="Key"/> rather than by <see cref="Nodes"/>, because
/// <see cref="ImmutableArray{T}"/>.Equals compares the underlying array reference and not the
/// elements, so two independently built paths naming the same chain would otherwise compare
/// unequal. Correct equality matters: the classifier groups candidate classifications by this
/// value and picks the most-supported one, so reference equality would degrade that ranking.
/// </remarks>
public readonly struct CategoryPath : IEquatable<CategoryPath>
{
    /// <summary>ASCII unit separator; cannot occur in a category name, so it cannot make two distinct paths compare equal.</summary>
    private const char Separator = '';

    private readonly ImmutableArray<UnlockCategory> _nodes;
    private readonly string _key;

    public CategoryPath(ImmutableArray<UnlockCategory> nodes)
    {
        _nodes = nodes;
        _key = string.Join(Separator, nodes.Select(n => n.Name));
    }

    /// <summary>The categories from the root to the node itself. The last entry is the node itself.</summary>
    public ImmutableArray<UnlockCategory> Nodes => _nodes;

    /// <summary>Canonical identity of this path, used for equality, hashing and logging.</summary>
    public string Key => _key;

    public int Depth => _nodes.Length;

    public bool IsRoot => _nodes.Length == 1;

    /// <summary>The node this path points at.</summary>
    public UnlockCategory Leaf => _nodes[^1];

    public bool Equals(CategoryPath other) => string.Equals(Key, other.Key, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is CategoryPath other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);

    public override string ToString() => Key.Replace(Separator, '/');

    public static bool operator ==(CategoryPath left, CategoryPath right) => left.Equals(right);

    public static bool operator !=(CategoryPath left, CategoryPath right) => !left.Equals(right);
}
