using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Rules;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BotSharp.Abstraction.Graph.Utils;

public static class GraphBuilder
{
    public static FlowGraph Build(GraphQueryResult result, IDictionary<string, string>? data = null)
    {
        var graph = FlowGraph.Init();
        if (result.Values.IsNullOrEmpty())
        {
            return graph;
        }

        foreach (var item in result.Values)
        {
            // Try to deserialize nodes and edge from the dictionary
            if (!item.TryGetValue<JsonElement>("sourceNode", out var sourceNodeElement) ||
                !item.TryGetValue<JsonElement>("targetNode", out var targetNodeElement) ||
                !item.TryGetValue<JsonElement>("edge", out var edgeElement))
            {
                continue;
            }

            // Parse source node
            var sourceNodeId = sourceNodeElement.TryGetProperty("id", out var sId) ? sId.GetString() : null;
            var sourceNodeLabels = sourceNodeElement.TryGetProperty("labels", out var sLabels)
                ? sLabels.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : [];
            var sourceNodeProps = sourceNodeElement.TryGetProperty("properties", out var sProps)
                ? sProps
                : default;

            // Parse target node
            var targetNodeId = targetNodeElement.TryGetProperty("id", out var tId) ? tId.GetString() : null;
            var targetNodeLabels = targetNodeElement.TryGetProperty("labels", out var tLabels)
                ? tLabels.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : [];
            var targetNodeProps = targetNodeElement.TryGetProperty("properties", out var tProps)
                ? tProps
                : default;

            // Parse edge
            var edgeId = edgeElement.TryGetProperty("id", out var eId) ? eId.GetString() : null;
            var edgeProps = edgeElement.TryGetProperty("properties", out var eProps)
                ? eProps
                : default;

            // Create source node
            var sourceNode = new FlowNode()
            {
                Id = sourceNodeId ?? Guid.NewGuid().ToString(),
                Labels = sourceNodeLabels,
                Name = GetGraphItemAttribute(sourceNodeProps, key: "name", data: data),
                Type = GetGraphItemAttribute(sourceNodeProps, key: "type", data: data),
                Description = GetGraphItemAttribute(sourceNodeProps, key: "description", data: data),
                Config = GetConfig(sourceNodeProps, data)
            };

            // Create target node
            var targetNode = new FlowNode()
            {
                Id = targetNodeId ?? Guid.NewGuid().ToString(),
                Labels = targetNodeLabels,
                Name = GetGraphItemAttribute(targetNodeProps, key: "name", data: data),
                Type = GetGraphItemAttribute(targetNodeProps, key: "type", data: data),
                Description = GetGraphItemAttribute(targetNodeProps, key: "description", data: data),
                Config = GetConfig(targetNodeProps, data)
            };

            // Create edge payload
            var edgePayload = new EdgeItemPayload()
            {
                Id = edgeId ?? Guid.NewGuid().ToString(),
                Name = GetGraphItemAttribute(edgeProps, key: "name", data: data),
                Type = GetGraphItemAttribute(edgeProps, key: "type", defaultValue: "NEXT", data: data),
                Description = GetGraphItemAttribute(edgeProps, key: "description", data: data),
                Config = GetConfig(edgeProps, data)
            };

            // Add edge to graph
            graph.AddEdge(sourceNode, targetNode, edgePayload);
        }

        return graph;
    }

    private static string GetGraphItemAttribute(JsonElement? properties, string key, string defaultValue = null, IDictionary<string, string>? data = null)
    {
        if (properties == null || properties.Value.ValueKind == JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        if (properties.Value.TryGetProperty(key, out var name) && name.ValueKind == JsonValueKind.String)
        {
            return ReplacePlaceholders(name.GetString(), data) ?? defaultValue;
        }

        return defaultValue;
    }

    private static Dictionary<string, string?> GetConfig(JsonElement? properties, IDictionary<string, string>? data = null)
    {
        var config = new Dictionary<string, string?>();

        if (properties == null || properties.Value.ValueKind == JsonValueKind.Undefined)
        {
            return config;
        }

        // Convert all properties to config dictionary
        foreach (var prop in properties.Value.EnumerateObject())
        {
            config[prop.Name] = ReplacePlaceholders(prop.Value.ConvertToString(), data);
        }

        return config;
    }

    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    private static string? ReplacePlaceholders(string? value, IDictionary<string, string>? data)
    {
        if (data == null || string.IsNullOrEmpty(value) || !value.Contains("{{"))
        {
            return value;
        }

        return PlaceholderRegex.Replace(value, match =>
        {
            var key = match.Groups[1].Value;
            return data.TryGetValue(key, out var replacement) && replacement != null
                ? replacement
                : match.Value;
        });
    }
}
