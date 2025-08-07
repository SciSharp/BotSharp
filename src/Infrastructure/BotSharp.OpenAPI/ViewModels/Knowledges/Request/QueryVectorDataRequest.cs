namespace BotSharp.OpenAPI.ViewModels.Knowledges.Request;

public class QueryVectorDataRequest
{
    public List<string> Ids { get; set; } = [];
    public bool WithVector { get; set; }
    public bool WithPayload { get; set; }
}
