using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging;

public interface ITemplateMessage
{
    [JsonPropertyName("template_type")]
    [JsonProperty("template_type")]
    string TemplateType => string.Empty;
}
