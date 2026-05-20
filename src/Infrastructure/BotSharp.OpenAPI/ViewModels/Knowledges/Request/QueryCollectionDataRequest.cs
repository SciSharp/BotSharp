namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class QueryCollectionDataRequest
{
    public List<string> Ids { get; set; } = [];
    public bool WithVector { get; set; }
    public bool WithPayload { get; set; }
    public string? DbProvider { get; set; }
}
