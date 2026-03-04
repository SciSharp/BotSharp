namespace BotSharp.Abstraction.Agents.Models;

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

    public RuleNode? GetRootNode()
    {
        return _nodes.FirstOrDefault(x => x.Type.IsEqualTo("root") || x.Type.IsEqualTo("start"));
    }

    public RuleNode? GetNode(string id)
    {
        return _nodes.FirstOrDefault(x => x.Id.IsEqualTo(id));
    }

    public string GetGraphId()
    {
        return _id;
    }

    public IEnumerable<RuleNode> GetNodes()
    {
        return [.. _nodes];
    }

    public IEnumerable<RuleEdge> GetEdges()
    {
        return [.. _edges];
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

    public void AddEdge(RuleNode from, RuleNode to, GraphItemPayload payload)
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
                Config = payload.Config
            });
        }
    }

    public IEnumerable<(RuleNode, RuleEdge)> GetNeighbors(RuleNode node)
    {
        return _edges.Where(e => e.From != null && e.From.Id.IsEqualTo(node.Id))
                     .Select(e => (e.To, e))
                     .ToList();
    }

    public RuleGraphInfo GetGraphInfo()
    {
        return new()
        {
            GraphId = _id,
            Nodes = _nodes,
            Edges = _edges
        };
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

public class RuleNode : GraphItemPayload
{
    /// <summary>
    /// Node type: root, criteria, action, etc.
    /// </summary>
    public override string Type { get; set; } = "action";

    public override string ToString()
    {
        return $"Node ({Id}): {Name} ({Type})";
    }
}

public class RuleEdge : GraphItemPayload
{
    /// <summary>
    /// Edge type: is_next, etc.
    /// </summary>
    public override string Type { get; set; } = "is_next";

    public RuleNode From { get; set; }
    public RuleNode To { get; set; }

    public RuleEdge()
    {
        
    }

    public RuleEdge(RuleNode from, RuleNode to)
    {
        From = from;
        To = to;
    }

    public override string ToString()
    {
        return $"Edge ({Id}): {Name} ({Type}), Connects from Node ({From?.Name}) to Node ({To?.Name})";
    }
}

public class GraphItemPayload
{
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();
    public virtual string Name { get; set; } = null!;
    public virtual string Type { get; set; } = null!;
    public virtual IEnumerable<string> Labels { get; set; } = [];
    public virtual Dictionary<string, string?> Config { get; set; } = [];

    public GraphItemPayload()
    {
        
    }
}

public class RuleGraphInfo
{
    public string GraphId { get; set; }
    public IEnumerable<RuleNode> Nodes { get; set; } = [];
    public IEnumerable<RuleEdge> Edges { get; set; } = [];
}