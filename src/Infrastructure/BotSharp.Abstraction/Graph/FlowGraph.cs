using BotSharp.Abstraction.Graph.Models;

namespace BotSharp.Abstraction.Rules;

public class FlowGraph
{
    private string _id = Guid.NewGuid().ToString();
    private List<FlowNode> _nodes = [];
    private List<FlowEdge> _edges = [];

    public FlowGraph()
    {
        _id = Guid.NewGuid().ToString();
        _nodes = [];
        _edges = [];
    }

    public static FlowGraph Init()
    {
        return new FlowGraph();
    }

    public FlowNode? GetRootNode(string? name = null)
    {
        if (!string.IsNullOrEmpty(name))
        {
            return _nodes.FirstOrDefault(x => x.Name.IsEqualTo(name));
        }

        return _nodes.FirstOrDefault(x => x.Type.IsEqualTo("root") || x.Type.IsEqualTo("start"));
    }

    public (FlowNode? Node, IEnumerable<FlowEdge> IncomingEdges, IEnumerable<FlowEdge> OutgoingEdges) GetNode(string id)
    {
        var node = _nodes.FirstOrDefault(x => x.Id.IsEqualTo(id));
        if (node == null)
        {
            return (null, [], []);
        }

        var incomingEdges = _edges
            .Where(e => e.To != null && e.To.Id.IsEqualTo(id))
            .OrderByDescending(x => x.Weight)
            .ToList();
        var outgoingEdges = _edges
            .Where(e => e.From != null && e.From.Id.IsEqualTo(id))
            .OrderByDescending(x => x.Weight)
            .ToList();

        return (node, incomingEdges, outgoingEdges);
    }

    public string GetGraphId()
    {
        return _id;
    }

    public IEnumerable<FlowNode> GetNodes(Func<FlowNode, bool>? filter = null)
    {
        return filter == null ? [.. _nodes] : [.. _nodes.Where(filter)];
    }

    public IEnumerable<FlowEdge> GetEdges(Func<FlowEdge, bool>? filter = null)
    {
        return filter == null ? [.. _edges] : [.. _edges.Where(filter)];
    }

    public void SetGraphId(string id)
    {
        _id = id;
    }

    public void SetNodes(IEnumerable<FlowNode> nodes)
    {
        _nodes = [.. nodes?.ToList() ?? []];
    }

    public void SetEdges(IEnumerable<FlowEdge> edges)
    {
        _edges = [.. edges?.ToList() ?? []];
    }

    public void AddNode(FlowNode node)
    {
        var found = _nodes.Exists(x => x.Id.IsEqualTo(node.Id));
        if (!found)
        {
            _nodes.Add(node);
        }
    }

    public void AddEdge(FlowNode from, FlowNode to, EdgeItemPayload payload)
    {
        var sourceFound = _nodes.Exists(x => x.Id.IsEqualTo(from.Id));
        var targetFound = _nodes.Exists(x => x.Id.IsEqualTo(to.Id));
        var edgeFound = _edges.Exists(x => x.Id.IsEqualTo(payload.Id));

        if (!sourceFound)
        {
            _nodes.Add(from);
        }

        if (!targetFound)
        {
            _nodes.Add(to);
        }

        if (!edgeFound)
        {
            _edges.Add(new FlowEdge(from, to)
            {
                Id = payload.Id,
                Name = payload.Name,
                Type = payload.Type,
                Labels = [.. payload.Labels ?? []],
                Weight = payload.Weight,
                Alias = payload.Alias,
                Description = payload.Description,
                Config = new(payload.Config ?? [])
            });
        }
    }

    public IEnumerable<(FlowNode, FlowEdge)> GetParentNodes(FlowNode node, bool ascending = false)
    {
        var filtered = _edges.Where(e => e.To != null && e.To.Id.IsEqualTo(node.Id));
        var ordered = ascending
            ? filtered.OrderBy(e => e.Weight)
            : filtered.OrderByDescending(e => e.Weight);

        return ordered.Select(e => (e.From, e)).ToList();
    }

    public IEnumerable<(FlowNode, FlowEdge)> GetChildrenNodes(FlowNode node, bool ascending = false)
    {
        var filtered = _edges.Where(e => e.From != null && e.From.Id.IsEqualTo(node.Id));
        var ordered = ascending
            ? filtered.OrderBy(e => e.Weight)
            : filtered.OrderByDescending(e => e.Weight);

        return ordered.Select(e => (e.To, e)).ToList();
    }

    public FlowGraphInfo GetGraphInfo()
    {
        return new()
        {
            GraphId = _id,
            Nodes = [.. _nodes?.ToList() ?? []],
            Edges = [.. _edges?.ToList() ?? []]
        };
    }

    public void Clear()
    {
        _nodes = [];
        _edges = [];
    }

    public static FlowGraph FromGraphInfo(FlowGraphInfo graphInfo)
    {
        var graph = new FlowGraph();
        graph.SetGraphId(graphInfo.GraphId.IfNullOrEmptyAs(Guid.NewGuid().ToString())!);
        graph.SetNodes(graphInfo.Nodes);
        graph.SetEdges(graphInfo.Edges);
        return graph;
    }

    public override string ToString()
    {
        return $"Graph ({_id}) => Nodes: {_nodes.Count}, Edges: {_edges.Count}";
    }
}

public class FlowNode : GraphItem
{
    /// <summary>
    /// Node type: root, criteria, action, etc.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public override string Type { get; set; } = "action";

    /// <summary>
    /// Input schema loaded from node config. Overrides the code-defined schema on IRuleFlowUnit.
    /// </summary>
    [JsonPropertyName("input_schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlowUnitSchema? InputSchema { get; set; }

    /// <summary>
    /// Output schema loaded from node config. Overrides the code-defined schema on IRuleFlowUnit.
    /// </summary>
    [JsonPropertyName("output_schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlowUnitSchema? OutputSchema { get; set; }

    public override string ToString()
    {
        return $"Node ({Id}): {Name} ({Type} => {Description})";
    }
}

public class FlowEdge : GraphItem
{
    /// <summary>
    /// Edge type: is_next, etc.
    /// </summary>
    public override string Type { get; set; } = "next";

    public FlowNode From { get; set; }
    public FlowNode To { get; set; }

    public FlowEdge() : base()
    {

    }

    public FlowEdge(FlowNode from, FlowNode to) : base()
    {
        Id = Guid.NewGuid().ToString();
        From = from;
        To = to;
    }

    public override string ToString()
    {
        return $"Edge ({Id}): {Name} ({Type} => {Description}), Connects from Node ({From?.Name}) to Node ({To?.Name})";
    }
}

public class GraphItem
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string Name { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string Type { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual IEnumerable<string> Labels { get; set; } = [];
    public virtual double Weight { get; set; } = 1.0;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual Dictionary<string, string?> Config { get; set; } = [];

    private string? _alias;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string Alias
    {
        get => string.IsNullOrEmpty(_alias) ? Name : _alias;
        set => _alias = value;
    }
}

public class NodeItem : GraphItem
{

}

public class EdgeItem : GraphItem
{

}

public class NodeItemPayload : GraphItem
{

}

public class EdgeItemPayload : GraphItem
{

}

public class FlowGraphInfo
{
    [JsonPropertyName("graph_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string GraphId { get; set; }
    public IEnumerable<FlowNode> Nodes { get; set; } = [];
    public IEnumerable<FlowEdge> Edges { get; set; } = [];
}