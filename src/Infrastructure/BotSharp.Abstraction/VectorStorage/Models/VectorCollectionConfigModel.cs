namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCollectionConfigsModel
{
    [JsonPropertyName("collections")]
    public List<VectorCollectionConfig> Collections { get; set; } = new();
}

public class VectorCollectionConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text_embedding")]
    public KnowledgeEmbeddingConfig TextEmbedding { get; set; }

    [JsonPropertyName("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("create_user_id")]
    public string CreateUserId { get; set; } = string.Empty;
}

public class KnowledgeEmbeddingConfig
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("dimension")]
    public int Dimension { get; set; }
}