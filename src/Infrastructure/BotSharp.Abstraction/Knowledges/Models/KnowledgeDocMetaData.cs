namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeDocMetaData
{
    [JsonPropertyName("collection")]
    public string Collection { get; set; }

    [JsonPropertyName("file_id")]
    public Guid FileId { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("vector_store_provider")]
    public string VectorStoreProvider { get; set; }

    [JsonPropertyName("vector_data_ids")]
    public IEnumerable<string> VectorDataIds { get; set; } = new List<string>();

    [JsonPropertyName("ref_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocMetaRefData? RefData { get; set; }

    [JsonPropertyName("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("create_user_id")]
    public string CreateUserId { get; set; }
}

public class DocMetaRefData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("json_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JsonContent { get; set; }
}