using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Gw2Unlocks.WikiProcessing;

public static class AcquisitionGraphUtils
{
    public static string ToKey(this NodeId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return id.Name;
    }
}

public enum NodeType
{
    None = 0,

    // Core
    Item = 1,
    NPC = 2,
    Skin = 3,
    Location = 4,
    Gw2Object = 5,
    // Specializations
    Weapon = 10,
    Armor = 11,
    BackItem = 12,
    // Location subtypes
    Area = 20,
    Zone = 21,
    Region = 22,
    City = 23,
    // Semantic
    Container = 30
}


public static class NodeTypeExtensions
{
    public static string ToMetadata(this NodeType type)
        => type.ToString();
}

public record NodeId(string Name);

public class Node
{
    [JsonInclude]
    public Dictionary<string, string> Metadata { get; private set; } = [];

    public NodeType Type { get; set; } = NodeType.None;
    public bool IsProcessed { get; set; }

    //for json deserialisation
    public Node() { }

    public Node(Dictionary<string, string> metadata)
    {
        if(metadata != null)
            Metadata = metadata;
    }

    public void SetType(NodeType type)
    {
        if (Type == NodeType.None)
        {
            Type = type;
        }
    }

    public void UpdateMetadata(Dictionary<string, string> newMetadata)
    {
        ArgumentNullException.ThrowIfNull(newMetadata);
        foreach (var kvp in newMetadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }
    }
}

public enum EdgeType
{
    None = 0,
    SoldBy = 1,
    LocatedIn = 2,
    ContainedIn = 3,
    Rewards = 4,
    SkinUnlock = 5,
    HasIngredient = 6,
    GatheredFrom = 7,
}

public sealed record Edge(
    string From,
    string To,
    EdgeType Type,
    Dictionary<string, string>? Metadata = null
);
public class AcquisitionGraph
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public Dictionary<string, Node> Nodes { get; set; } = [];
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public HashSet<Edge> Edges { get; set; } = [];

    public Node GetOrCreate(string nodeId, Dictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        nodeId = Normalize(nodeId);

        if (!Nodes.TryGetValue(nodeId, out var node))
        {
            node = new Node(metadata ?? []);
            Nodes[nodeId] = node;
        }
        else if (metadata != null)
        {
            node.UpdateMetadata(metadata);
        }

        return node;
    }

    public void RemoveNodeAndAllEdges(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        // Remove edges where this node is either the source or target
        Edges.RemoveWhere(e => e.From == nodeId || e.To == nodeId);

        // Remove the node itself
        Nodes.Remove(nodeId);
    }

    public IEnumerable<Edge> GetIncomingEdges(string nodeId)
    {
        return Edges.Where(e => e.To.Equals(nodeId, StringComparison.Ordinal));
    }

    public IEnumerable<Edge> GetOutgoingEdges(string nodeId)
    {
        return Edges.Where(e => e.From.Equals(nodeId, StringComparison.Ordinal));
    }

    public IEnumerable<Edge> GetAllEdges(NodeId nodeId)
    {
        return Edges.Where(e => e.To.Equals(nodeId)).Union(Edges.Where(e => e.From.Equals(nodeId)));
    }

    public Node? GetNode(string name)
    {
        Nodes.TryGetValue(name, out var node);
        return node;
    }

    public Node? GetNode(string name, NodeType type)
    {
        if (!Nodes.TryGetValue(name, out var node))
            return null;

        return node.Type == type ? node : null;
    }

    private static string Normalize(string name)
    {
        return name.Split('#')[0].Trim();
    }

    public void AddEdge(string fromId, string toId, EdgeType type, Dictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toId);

        GetOrCreate(fromId);
        GetOrCreate(toId);

        Edges.Add(new Edge(fromId, toId, type, metadata));
    }
}