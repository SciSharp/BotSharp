using System.Net.Http;

namespace BotSharp.Abstraction.Rules.Models;

public class RuleHttpContext
{
    public string BaseUrl { get; set; }
    public string RelativeUrl { get; set; }
    public HttpMethod Method { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public Dictionary<string, string?> QueryParams { get; set; } = [];
    public string RequestBody { get; set; }
}
