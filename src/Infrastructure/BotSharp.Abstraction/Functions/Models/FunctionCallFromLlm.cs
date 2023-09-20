using BotSharp.Abstraction.Routing.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

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
