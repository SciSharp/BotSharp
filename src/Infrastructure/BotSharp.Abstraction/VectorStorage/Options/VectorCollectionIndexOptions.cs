namespace BotSharp.Abstraction.VectorStorage.Options;

public class VectorCollectionIndexOptions
{
    [JsonPropertyName("field_name")]
    public string FieldName { get; set; } = null!;
}

public class CreateVectorCollectionIndexOptions : VectorCollectionIndexOptions
{
    [JsonPropertyName("field_schema_type")]
    public string FieldSchemaType { get; set; } = null!;
}

public class DeleteVectorCollectionIndexOptions : VectorCollectionIndexOptions
{

}