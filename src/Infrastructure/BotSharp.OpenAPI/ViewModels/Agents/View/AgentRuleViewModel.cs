using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentRuleViewModel
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; } = string.Empty;

    [JsonPropertyName("output_args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonDocument? OutputArgs { get; set; }

    [JsonPropertyName("json_args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JsonArgs
    {
        get
        {
            if (OutputArgs == null)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(OutputArgs.RootElement, new JsonSerializerOptions { WriteIndented = true });
            return $"```json\r\n{json}\r\n```";
        }
    }
}
