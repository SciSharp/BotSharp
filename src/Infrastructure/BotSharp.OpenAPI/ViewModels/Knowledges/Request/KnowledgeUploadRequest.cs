namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeUploadRequest
{
    /// <summary>
    /// Provider for knowledge file orchestrator
    /// </summary>
    public string? FileOrchestrator { get; set; }

    public IEnumerable<ExternalFileModel> Files { get; set; } = new List<ExternalFileModel>();

    public KnowledgeFileHandleOptions? Options { get; set; }
}
