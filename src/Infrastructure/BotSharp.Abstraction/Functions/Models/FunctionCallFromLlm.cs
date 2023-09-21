using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionCallFromLlm
{
    [JsonPropertyName("function")]
    public string Function { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public RetrievalArgs Parameters { get; set; } = new RetrievalArgs();

    public override string ToString()
    {
        return $"{Function} ({Reason}) {Parameters}";
    }
}
