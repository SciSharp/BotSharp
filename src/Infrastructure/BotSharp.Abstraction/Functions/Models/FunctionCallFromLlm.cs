using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionCallFromLlm
{
    [JsonPropertyName("function")]
    public string Function { get; set; }

    [JsonPropertyName("parameters")]
    public RetrievalArgs Parameters { get; set; }

    public override string ToString()
    {
        return $"{Function} {Parameters}";
    }
}
