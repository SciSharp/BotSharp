using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeFileViewModel
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_extension")]
    public string FileExtension { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }

    public static KnowledgeFileViewModel From(KnowledgeFileModel model)
    {
        return new KnowledgeFileViewModel
        {
            FileId = model.FileId,
            FileName = model.FileName,
            FileExtension = model.FileExtension,
            ContentType = model.ContentType,
            FileUrl = model.FileUrl
        };
    }
}
