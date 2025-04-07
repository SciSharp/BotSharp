namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class RealtimeSessionBody
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Id { get; set; } = null!;

    [JsonPropertyName("object")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Object { get; set; } = null!;

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Model { get; set; } = null!;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.8f;

    [JsonPropertyName("modalities")]
    public string[] Modalities { get; set; } = ["audio", "text"];

    [JsonPropertyName("input_audio_format")]
    public string InputAudioFormat { get; set; } = null!;

    [JsonPropertyName("output_audio_format")]
    public string OutputAudioFormat { get; set; } = null!;

    [JsonPropertyName("input_audio_transcription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InputAudioTranscription? InputAudioTranscription { get; set; }

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = "You are a friendly assistant.";

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "sage";

    [JsonPropertyName("max_response_output_tokens")]
    public int MaxResponseOutputTokens { get; set; } = 512;

    [JsonPropertyName("tool_choice")]
    public string ToolChoice { get; set; } = "auto";

    [JsonPropertyName("tools")]
    public FunctionDef[] Tools { get; set; } = [];

    [JsonPropertyName("turn_detection")]
    public RealtimeSessionTurnDetection? TurnDetection { get; set; } = new();

    [JsonPropertyName("input_audio_noise_reduction")]
    public InputAudioNoiseReduction InputAudioNoiseReduction { get; set; } = new();
}

public class RealtimeSessionTurnDetection
{
    [JsonPropertyName("interrupt_response")]
    public bool InterruptResponse { get; set; } = true;

    /// <summary>
    /// Milliseconds
    /// </summary>
    /*[JsonPropertyName("prefix_padding_ms")]
    public int PrefixPadding { get; set; } = 300;

    [JsonPropertyName("silence_duration_ms")]
    public int SilenceDuration { get; set; } = 500;

    [JsonPropertyName("threshold")]
    public float Threshold { get; set; } = 0.5f;*/

    /// <summary>
    /// server_vad, semantic_vad
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "semantic_vad";

    [JsonPropertyName("eagerness")]
    public string eagerness { get;set; } = "auto";
}

public class InputAudioTranscription
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-transcribe";

    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prompt { get; set; }
}

public class InputAudioNoiseReduction
{
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Type { get; set; } = "far_field";
}