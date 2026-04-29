namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeUploadRequest
{
    /// <summary>
    /// Provider for knowledge file orchestrator
    /// </summary>
    public string? Orchestrator { get; set; }

    public IEnumerable<ExternalFileModel> Files { get; set; } = new List<ExternalFileModel>();

    public KnowledgeFileHandleOptions? Options { get; set; }
}
