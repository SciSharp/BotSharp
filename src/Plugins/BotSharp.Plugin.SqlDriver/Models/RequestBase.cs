using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class RequestBase
{
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}


public class ImportDbKnowledgeRequest : RequestBase
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; }

    [JsonPropertyName("knowledgebase_collection")]
    public string KnowledgebaseCollection { get; set; }
}