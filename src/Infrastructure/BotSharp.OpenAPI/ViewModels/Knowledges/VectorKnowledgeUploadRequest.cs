namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeUploadRequest
{
    public IEnumerable<InputFileModel> Files { get; set; } = new List<InputFileModel>();
}
