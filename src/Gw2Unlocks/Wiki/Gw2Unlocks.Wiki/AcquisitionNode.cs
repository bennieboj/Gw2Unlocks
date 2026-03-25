using System;
using System.Collections.Generic;

namespace Gw2Unlocks.Wiki;

public enum NodeType
{
    Item,
    Vendor,
    Achievement,
    Container,
    Skin,
    Area,
    Zone
}

public record NodeId(NodeType Type, string Name);

public record Node(NodeId Id);

public enum EdgeType
{
    SoldBy,
    LocatedIn,
    Contains,
    Rewards,
    SkinUnlock
}

public sealed record Edge(
    NodeId From,
    NodeId To,
    EdgeType Type,
    Dictionary<string, string>? Metadata = null
);
public class AcquisitionGraph
{
    private readonly Dictionary<NodeId, Node> _nodes = [];
    private readonly HashSet<Edge> _edges = [];

    public IReadOnlyCollection<Node> Nodes => _nodes.Values;
    public IReadOnlyCollection<Edge> Edges => _edges;

    public Node GetOrCreate(NodeType type, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        name = Normalize(name);
        var id = new NodeId(type, name);

        if (!_nodes.TryGetValue(id, out var node))
        {
            node = new Node(id);
            _nodes[id] = node;
        }

        return node;
    }

    private static string Normalize(string name)
    {
        return name.Split('#')[0].Trim();
    }

    public void AddEdge(NodeId from, NodeId to, EdgeType type, Dictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var fromNode = GetOrCreate(from.Type, from.Name);
        var toNode = GetOrCreate(to.Type, to.Name);

        _edges.Add(new Edge(fromNode.Id, toNode.Id, type, metadata));
    }
}