using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Gw2Unlocks.Wiki;

public static class AcquisitionGraphUtils
{
    public static string ToKey(this NodeId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return $"{id.Type}:{id.Name}";
    }
}

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

public class Node
{
    // = default! is for json deserialisation
    public NodeId Id { get; init; } = default!;
    public bool IsProcessed { get; set; }

    //for json deserialisation
    public Node() { }

    public Node(NodeId id)
    {
        Id = id;
    }
}

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public Dictionary<string, Node> Nodes { get; set; } = [];
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public HashSet<Edge> Edges { get; set; } = [];

    public Node GetOrCreate(NodeType type, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        name = Normalize(name);
        var id = new NodeId(type, name);

        var idString = id.ToKey();
        if (!Nodes.TryGetValue(idString, out var node))
        {
            node = new Node(id);
            Nodes[idString] = node;
        }

        return node;
    }

    public Node? GetNode(NodeType type, string name)
    {
        Nodes.TryGetValue($"{type}:{name}", out var node);
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

        Edges.Add(new Edge(fromNode.Id, toNode.Id, type, metadata));
    }
}