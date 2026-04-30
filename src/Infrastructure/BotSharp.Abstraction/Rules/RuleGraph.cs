using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

public class RuleGraph
{
    private string _id = Guid.NewGuid().ToString();
    private List<RuleNode> _nodes = [];
    private List<RuleEdge> _edges = [];

    public RuleGraph()
    {
        _id = Guid.NewGuid().ToString();
        _nodes = [];
        _edges = [];
    }

    public static RuleGraph Init()
    {
        return new RuleGraph();
    }

    public RuleNode? GetRootNode(string? name = null)
    {
        if (!string.IsNullOrEmpty(name))
        {
            return _nodes.FirstOrDefault(x => x.Name.IsEqualTo(name));
        }

        return _nodes.FirstOrDefault(x => x.Type.IsEqualTo("root") || x.Type.IsEqualTo("start"));
    }

    public (RuleNode? Node, IEnumerable<RuleEdge> IncomingEdges, IEnumerable<RuleEdge> OutgoingEdges) GetNode(string id)
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

    public IEnumerable<RuleNode> GetNodes(Func<RuleNode, bool>? filter = null)
    {
        return filter == null ? [.. _nodes] : [.. _nodes.Where(filter)];
    }

    public IEnumerable<RuleEdge> GetEdges(Func<RuleEdge, bool>? filter = null)
    {
        return filter == null ? [.. _edges] : [.. _edges.Where(filter)];
    }

    public void SetGraphId(string id)
    {
        _id = id;
    }

    public void SetNodes(IEnumerable<RuleNode> nodes)
    {
        _nodes = [.. nodes?.ToList() ?? []];
    }

    public void SetEdges(IEnumerable<RuleEdge> edges)
    {
        _edges = [.. edges?.ToList() ?? []];
    }

    public void AddNode(RuleNode node)
    {
        var found = _nodes.Exists(x => x.Id.IsEqualTo(node.Id));
        if (!found)
        {
            _nodes.Add(node);
        }
    }

    public void AddEdge(RuleNode from, RuleNode to, EdgeItemPayload payload)
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
            _edges.Add(new RuleEdge(from, to)
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

    public IEnumerable<(RuleNode, RuleEdge)> GetParentNodes(RuleNode node, bool ascending = false)
    {
        var filtered = _edges.Where(e => e.To != null && e.To.Id.IsEqualTo(node.Id));
        var ordered = ascending
            ? filtered.OrderBy(e => e.Weight)
            : filtered.OrderByDescending(e => e.Weight);

        return ordered.Select(e => (e.From, e)).ToList();
    }

    public IEnumerable<(RuleNode, RuleEdge)> GetChildrenNodes(RuleNode node, bool ascending = false)
    {
        var filtered = _edges.Where(e => e.From != null && e.From.Id.IsEqualTo(node.Id));
        var ordered = ascending
            ? filtered.OrderBy(e => e.Weight)
            : filtered.OrderByDescending(e => e.Weight);

        return ordered.Select(e => (e.To, e)).ToList();
    }

    public RuleGraphInfo GetGraphInfo()
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

    public static RuleGraph FromGraphInfo(RuleGraphInfo graphInfo)
    {
        var graph = new RuleGraph();
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

public class RuleNode : GraphItem
{
    /// <summary>
    /// Node type: root, criteria, action, etc.
    /// </summary>
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

public class RuleEdge : GraphItem
{
    /// <summary>
    /// Edge type: is_next, etc.
    /// </summary>
    public override string Type { get; set; } = "next";

    public RuleNode From { get; set; }
    public RuleNode To { get; set; }

    public RuleEdge() : base()
    {
        
    }

    public RuleEdge(RuleNode from, RuleNode to) : base()
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
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();
    public virtual string Name { get; set; } = null!;
    public virtual string Type { get; set; } = null!;
    public virtual IEnumerable<string> Labels { get; set; } = [];
    public virtual double Weight { get; set; } = 1.0;
    public virtual string? Description { get; set; }
    public virtual Dictionary<string, string?> Config { get; set; } = [];

    private string? _alias;
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

public class RuleGraphInfo
{
    public string GraphId { get; set; }
    public IEnumerable<RuleNode> Nodes { get; set; } = [];
    public IEnumerable<RuleEdge> Edges { get; set; } = [];
}