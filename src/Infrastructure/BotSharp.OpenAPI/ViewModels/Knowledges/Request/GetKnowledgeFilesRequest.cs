using BotSharp.Abstraction.Knowledges.Filters;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class GetKnowledgeFilesRequest : KnowledgeFileFilter
{
    public string? FileOrchestrator { get; set; }
}
