namespace BotSharp.OpenAPI.ViewModels.Knowledges.Request;

public class QueryVectorDataRequest
{
    public IEnumerable<string> Ids { get; set; } = [];
    public bool WithVector { get; set; }
    public bool WithPayload { get; set; }
}
