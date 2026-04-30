using BotSharp.Abstraction.Rules;
using BotSharp.Abstraction.Rules.Models;
using BotSharp.Abstraction.Rules.Options;
using BotSharp.Abstraction.Utilities;

namespace BotSharp.Plugin.Membase.Services;

public class DemoRuleGraph : IRuleFlow<RuleGraph>
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

    public string Name => "Demo";

    public async Task<RuleConfigModel> GetTopologyConfigAsync(RuleFlowConfigOptions? options = null)
    {
        var settings = _services.GetRequiredService<MembaseSettings>();
        var apiKey = settings.ApiKey;
        var projectId = settings.ProjectId;

        var topologyName = Name;
        if (!string.IsNullOrEmpty(options?.TopologyName))
        {
            topologyName = options.TopologyName;
        }

        var foundInstance = settings.GraphInstances?.FirstOrDefault(x => x.Name.IsEqualTo(topologyName));
        var graphId = foundInstance?.Id ?? string.Empty;
        var query = Uri.EscapeDataString("MATCH (a)-[r]->(b) WITH a, r, b WHERE a.agent = $agent AND a.trigger = $trigger AND b.agent = $agent AND b.trigger = $trigger RETURN a, r, b LIMIT 100");

        return new RuleConfigModel
        {
            TopologyId = graphId,
            TopologyName = foundInstance?.Name,
            CustomParameters = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                htmlTag = "iframe",
                appendParameterName = "parameters",
                url = $"https://console.membase.dev/query-editor/{projectId}?graphId={graphId}&query={query}&token={apiKey}"
            }))
        };
    }

    public async Task<RuleGraph?> GetTopologyAsync(string id, RuleFlowLoadOptions? options = null)
    {
        if (string.IsNullOrEmpty(id))
        {
#if DEBUG
            return GetDefaultGraph();
#else
            return null;
#endif
        }

        var query = options?.Query ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            query = $"""
                MATCH (a)-[r]->(b)
                WITH a, r, b
                WHERE a.agent = $agent AND a.trigger = $trigger AND b.agent = $agent AND b.trigger = $trigger
                RETURN a, r, b 
                LIMIT 100
            """;
        }

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

        try
        {
            var graphDb = _services.GetServices<IGraphDb>().First(x => x.Provider.IsEqualTo("membase"));
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
            _logger.LogError(ex, "Error when loading graph (id: {GraphId})", id);
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
            var sourceNodeWeight = sourceNodeElement.TryGetProperty("weight", out var sNodeWeight) && sNodeWeight.ValueKind == JsonValueKind.Number
                ? sNodeWeight.GetDouble()
                : 1.0;

            // Parse target node
            var targetNodeId = targetNodeElement.GetProperty("id").GetString();
            var targetNodeLabels = targetNodeElement.TryGetProperty("labels", out var tLabels)
                ? tLabels.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : [];
            var targetNodeProps = targetNodeElement.TryGetProperty("properties", out var tProps)
                ? tProps
                : default;
            var targetNodeWeight = targetNodeElement.TryGetProperty("weight", out var tNodeWeight) && tNodeWeight.ValueKind == JsonValueKind.Number
                ? tNodeWeight.GetDouble()
                : 1.0;

            // Parse edge
            var edgeId = edgeElement.GetProperty("id").GetString();
            var edgeProps = edgeElement.TryGetProperty("properties", out var eProps)
                ? eProps
                : default;
            var edgeWeight = edgeElement.TryGetProperty("weight", out var eWeight) && eWeight.ValueKind == JsonValueKind.Number
                ? eWeight.GetDouble()
                : 1.0;

            // Create source node
            var sourceNode = new RuleNode()
            {
                Id = sourceNodeId ?? Guid.NewGuid().ToString(),
                Labels = sourceNodeLabels,
                Weight = sourceNodeWeight,
                Name = GetGraphItemAttribute(sourceNodeProps, key: "name", defaultValue: "node"),
                Type = GetGraphItemAttribute(sourceNodeProps, key: "type", defaultValue: "action"),
                Alias = GetGraphItemAttribute(sourceNodeProps, key: "alias", defaultValue: ""),
                Description = GetGraphItemAttribute(sourceNodeProps, key: "description", defaultValue: ""),
                Config = GetConfig(sourceNodeProps)
            };

            // Create target node
            var targetNode = new RuleNode()
            {
                Id = targetNodeId ?? Guid.NewGuid().ToString(),
                Labels = targetNodeLabels,
                Weight = targetNodeWeight,
                Name = GetGraphItemAttribute(targetNodeProps, key: "name", defaultValue: "node"),
                Type = GetGraphItemAttribute(targetNodeProps, key: "type", defaultValue: "action"),
                Alias = GetGraphItemAttribute(targetNodeProps, key: "alias", defaultValue: ""),
                Description = GetGraphItemAttribute(targetNodeProps, key: "description", defaultValue: ""),
                Config = GetConfig(targetNodeProps)
            };

            // Create edge payload
            var edgePayload = new EdgeItemPayload()
            {
                Id = edgeId ?? Guid.NewGuid().ToString(),
                Name = GetGraphItemAttribute(edgeProps, key: "name", defaultValue: "edge"),
                Type = GetGraphItemAttribute(edgeProps, key: "type", defaultValue: "next"),
                Alias = GetGraphItemAttribute(edgeProps, key: "alias", defaultValue: ""),
                Description = GetGraphItemAttribute(edgeProps, key: "description", defaultValue: ""),
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

        var node1 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://dummy.restapiexample.com/api/v1/employees"
            }
        };

        var node2 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://dummy.restapiexample.com/api/v1/employee/1"
            }
        };

        var node3 = new RuleNode
        {
            Name = "http_request",
            Type = "action",
            Config = new()
            {
                ["http_method"] = "GET",
                ["http_url"] = "https://dummy.restapiexample.com/api/v1/employee/2"
            }
        };

        graph.AddEdge(root, node1, payload: new()
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
