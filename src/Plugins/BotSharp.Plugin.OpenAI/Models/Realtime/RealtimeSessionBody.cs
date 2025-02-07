using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class RealtimeSessionBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("object")]
    public string Object { get; set; } = null!;

    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("temperature")]
    public float temperature { get; set; } = 0.8f;

    [JsonPropertyName("modalities")]
    public string[] Modalities { get; set; } = ["audio", "text"];

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = "You are a friendly assistant.";

    [JsonPropertyName("max_response_output_tokens")]
    public int MaxResponseOutputTokens { get; set; } = 512;

    [JsonPropertyName("tool_choice")]
    public string ToolChoice { get; set; } = "auto";

    [JsonPropertyName("tools")]
    public FunctionDef[] Tools { get; set; } = [];

    [JsonPropertyName("turn_detection")]
    public RealtimeSessionTurnDetection TurnDetection { get; set; } = new();
}

public class RealtimeSessionTurnDetection
{
    /// <summary>
    /// Milliseconds
    /// </summary>
    [JsonPropertyName("prefix_padding_ms")]
    public int PrefixPadding { get; set; } = 300;

    [JsonPropertyName("silence_duration_ms")]
    public int SilenceDuration { get; set; } = 500;

    [JsonPropertyName("threshold")]
    public float Threshold { get; set; } = 0.5f;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "server_vad";
}