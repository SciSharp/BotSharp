using System.Text.Json.Serialization;

namespace BotSharp.Plugin.PythonInterpreter.LlmContext;

public class LlmContextIn
{
    [JsonPropertyName("user_requirement")]
    public string UserRquirement { get; set; } = string.Empty;

    [JsonPropertyName("imported_packages")]
    public List<string> ImportedPackages { get; set; } = [];
}
