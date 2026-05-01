namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class CreateCollectionRequest : KnowledgeBaseRequestBase
{
    public string CollectionName { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public int Dimension { get; set; }
}
