using System.Text.Json.Serialization;

namespace BotSharp.Plugin.PythonInterpreter.LlmContext;

public class LlmContextOut
{
    [JsonPropertyName("python_code")]
    public string PythonCode { get; set; }

    [JsonPropertyName("imported_packages")]
    public List<string>? ImportedPackages { get; set; }
}
