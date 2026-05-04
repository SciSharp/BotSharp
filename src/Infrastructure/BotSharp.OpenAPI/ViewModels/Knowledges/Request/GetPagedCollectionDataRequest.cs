namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class GetPagedCollectionDataRequest : KnowledgeFilter
{
    public string KnowledgeType { get; set; } = null!;
}
