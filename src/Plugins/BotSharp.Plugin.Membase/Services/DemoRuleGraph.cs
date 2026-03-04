using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Options;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.Membase.Services;

public class DemoRuleGraph : IRuleGraph
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DemoRuleGraph> _logger;

    public DemoRuleGraph(
        IServiceProvider services,
        ILogger<DemoRuleGraph> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "demo";

    public Task<RuleGraph> GetGraphAsync(string graphId, RuleGraphLoadOptions? options = null)
    {
        var graph = RuleGraph.Init();
        var root = new RuleNode
        {
            Name = "start",
            Type = "start",
        };

        var end = new RuleNode
        {
            Name = "end",
            Type = "end",
        };

        var delayNode = new RuleNode
        {
            Name = "delay_message",
            Type = "action",
            Config = new()
            {
                ["delay"] = "3 seconds"
            }
        };

        var node1 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://meshstage.lessen.com/reactivewocore/reactivewos/9883958"
            }
        };

        var node2 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://meshstage.lessen.com/reactivewocore/reactivewos/9883956"
            }
        };

        var node3 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://meshstage.lessen.com/reactivewocore/reactivewos/9883954"
            }
        };

        graph.AddEdge(root, delayNode, payload: new()
        {
            Name = "edge",
            Type = "is_next"
        });

        graph.AddEdge(delayNode, node1, payload: new()
        {
            Name = "edge",
            Type = "next"
        });

        graph.AddEdge(node1, node2, payload: new()
        {
            Name = "edge",
            Type = "next"
        });

        graph.AddEdge(node1, node3, payload: new()
        {
            Name = "edge",
            Type = "next"
        });

        graph.AddEdge(node2, node3, payload: new()
        {
            Name = "edge",
            Type = "next"
        });

        graph.AddEdge(node3, end, payload: new()
        {
            Name = "edge",
            Type = "next"
        });

        return Task.FromResult(graph);
    }
}
