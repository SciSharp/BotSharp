namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class DeleteCollectionSnapshotRequest : KnowledgeBaseRequestBase
{
    public string SnapshotName { get; set; } = default!;
}
