namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeUploadRequest
{
    public IEnumerable<ExternalFileModel> Files { get; set; } = new List<ExternalFileModel>();
}
