using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

/// <summary>
/// Full configuration block returned by the pgt-definitions endpoint.
/// </summary>
public class PgtConfig
{
    [JsonPropertyName("startId")]
    public string? StartId { get; set; }

    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; }

    [JsonPropertyName("renderDepth")]
    public int RenderDepth { get; set; }

    [JsonPropertyName("appendTopology")]
    public bool AppendTopology { get; set; }

    [JsonPropertyName("fuzzyThreshold")]
    public double FuzzyThreshold { get; set; }

    [JsonPropertyName("maxNodes")]
    public int MaxNodes { get; set; }

    [JsonPropertyName("strategy")]
    public string? Strategy { get; set; }

    [JsonPropertyName("maxVisitsPerNode")]
    public int MaxVisitsPerNode { get; set; }

    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; set; }

    [JsonPropertyName("maxSubgraphNesting")]
    public int MaxSubgraphNesting { get; set; }

    [JsonPropertyName("recordTrace")]
    public bool RecordTrace { get; set; }

    [JsonPropertyName("persistRun")]
    public bool PersistRun { get; set; }

    [JsonPropertyName("validationChecks")]
    public List<string>? ValidationChecks { get; set; }

    [JsonPropertyName("targetIdsRaw")]
    public string? TargetIdsRaw { get; set; }

    [JsonPropertyName("allowedEdgeTypesRaw")]
    public string? AllowedEdgeTypesRaw { get; set; }

    /// <summary>JSON string containing the environment key-value pairs.</summary>
    [JsonPropertyName("environmentJson")]
    public string? EnvironmentJson { get; set; }

    /// <summary>JSON string containing the initial context key-value pairs.</summary>
    [JsonPropertyName("initialContextJson")]
    public string? InitialContextJson { get; set; }

    /// <summary>
    /// JSON array string of actor descriptors. Each element has an <c>actor_id</c>
    /// field used as the dictionary key when building a traverse request.
    /// </summary>
    [JsonPropertyName("actorsJson")]
    public string? ActorsJson { get; set; }

    [JsonPropertyName("dag")]
    public bool Dag { get; set; }
}
