using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Options;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BotSharp.Plugin.Membase.Services;

public class DemoRuleGraph : IRuleConfig<RuleGraph>
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

    public string Provider => "membase";

    public async Task<RuleGraph> GetConfigAsync(string id, RuleConfigLoadOptions? options = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var query = $"""
            MATCH (a)-[r]->(b)
            WITH DISTINCT a, r, b
            RETURN a, r, b 
            LIMIT 100
        """;

        var args = new Dictionary<string, object>();
        if (options?.Parameters != null)
        {
            foreach (var param in options.Parameters!)
            {
                if (param.Key == null || param.Value == null)
                {
                    continue;
                }
                args[param.Key] = param.Value;
            }
        }

        if (options?.AgentId != null)
        {
            args["agent_id"] = options.AgentId;
        }

        if (options?.Trigger != null)
        {
            args["trigger"] = options.Trigger;
        }

        try
        {
            var graphDb = _services.GetServices<IGraphDb>().First(x => x.Provider.IsEqualTo(Provider));
            var result = await graphDb.ExecuteQueryAsync(query, options: new()
            {
                GraphId = id,
                Arguments = args
            });

            if (result == null)
            {
                return null;
            }

            var graph = BuildGraph(result);
            return graph;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when loading graph (id: {GraphId}) for agent {AgentId} and trigger {Trigger} ",
                id, options?.AgentId, options?.Trigger);
            return null;
        }
    }

    private RuleGraph BuildGraph(GraphQueryResult result)
    {
        var graph = RuleGraph.Init();
        if (result.Values.IsNullOrEmpty())
        {
            return graph;
        }

        foreach (var item in result.Values)
        {
            // Try to deserialize nodes and edge from the dictionary
            if (!item.TryGetValue<JsonElement>("a", out var sourceNodeElement) ||
                !item.TryGetValue<JsonElement>("b", out var targetNodeElement) ||
                !item.TryGetValue<JsonElement>("r", out var edgeElement))
            {
                continue;
            }

            // Parse source node
            var sourceNodeId = sourceNodeElement.GetProperty("id").GetString();
            var sourceNodeLabels = sourceNodeElement.TryGetProperty("labels", out var sLabels)
                ? sLabels.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : [];
            var sourceNodeProps = sourceNodeElement.TryGetProperty("properties", out var sProps)
                ? sProps
                : default;

            // Parse target node
            var targetNodeId = targetNodeElement.GetProperty("id").GetString();
            var targetNodeLabels = targetNodeElement.TryGetProperty("labels", out var tLabels)
                ? tLabels.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : [];
            var targetNodeProps = targetNodeElement.TryGetProperty("properties", out var tProps)
                ? tProps
                : default;

            // Parse edge
            var edgeId = edgeElement.GetProperty("id").GetString();
            var edgeProps = edgeElement.TryGetProperty("properties", out var eProps)
                ? eProps
                : default;
            var edgeWeight = edgeElement.TryGetProperty("weight", out var eWeight) && eWeight.ValueKind == JsonValueKind.Number
                ? (int)eWeight.GetDouble()
                : 1;

            // Create source node
            var sourceNode = new RuleNode()
            {
                Id = sourceNodeId ?? Guid.NewGuid().ToString(),
                Labels = sourceNodeLabels,
                Name = GetGraphItemAttribute(sourceNodeProps, key: "name", defaultValue: "node"),
                Type = GetGraphItemAttribute(sourceNodeProps, key: "type", defaultValue: "action"),
                Config = GetConfig(sourceNodeProps)
            };

            // Create target node
            var targetNode = new RuleNode()
            {
                Id = targetNodeId ?? Guid.NewGuid().ToString(),
                Labels = targetNodeLabels,
                Name = GetGraphItemAttribute(targetNodeProps, key: "name", defaultValue: "node"),
                Type = GetGraphItemAttribute(targetNodeProps, key: "type", defaultValue: "action"),
                Config = GetConfig(targetNodeProps)
            };

            // Create edge payload
            var edgePayload = new GraphItemPayload()
            {
                Id = edgeId ?? Guid.NewGuid().ToString(),
                Name = GetGraphItemAttribute(targetNodeProps, key: "name", defaultValue: "edge"),
                Type = GetGraphItemAttribute(targetNodeProps, key: "type", defaultValue: "next"),
                Weight = edgeWeight,
                Config = GetConfig(edgeProps)
            };

            // Add edge to graph
            graph.AddEdge(sourceNode, targetNode, edgePayload);
        }

        return graph;
    }

    private string GetGraphItemAttribute(JsonElement? properties, string key, string defaultValue)
    {
        if (properties == null || properties.Value.ValueKind == JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        if (properties.Value.TryGetProperty(key, out var name) && name.ValueKind == JsonValueKind.String)
        {
            return name.GetString() ?? defaultValue;
        }

        return defaultValue;
    }

    private Dictionary<string, string?> GetConfig(JsonElement? properties)
    {
        var config = new Dictionary<string, string?>();

        if (properties == null || properties.Value.ValueKind == JsonValueKind.Undefined)
        {
            return config;
        }

        // Convert all properties to config dictionary
        foreach (var prop in properties.Value.EnumerateObject())
        {
            config[prop.Name] = prop.Value.ConvertToString();
        }

        return config;
    }

    private RuleGraph GetDefaultGraph()
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

        return graph;
    }
}
