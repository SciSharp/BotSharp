namespace BotSharp.Abstraction.VectorStorage.Options;

public class CollectionIndexOptions
{
    [JsonPropertyName("field_name")]
    public string FieldName { get; set; } = null!;

    [JsonPropertyName("field_schema_type")]
    public string? FieldSchemaType { get; set; }
}