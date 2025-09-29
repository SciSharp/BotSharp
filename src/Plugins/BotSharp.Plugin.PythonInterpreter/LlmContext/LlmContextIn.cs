using System.Text.Json.Serialization;

namespace BotSharp.Plugin.PythonInterpreter.LlmContext;

public class LlmContextIn
{
    [JsonPropertyName("user_requirement")]
    public string UserRquirement { get; set; }
}
