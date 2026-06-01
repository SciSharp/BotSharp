using BotSharp.Abstraction.Models;

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

    [JsonPropertyName("output_modalities")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? OutputModalities { get; set; } = ["audio"];

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = "You are a friendly assistant.";

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("tool_choice")]
    public string ToolChoice { get; set; } = "auto";

    [JsonPropertyName("tools")]
    public FunctionDef[] Tools { get; set; } = [];

    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RealtimeAudioConfig? Audio { get; set; }

    [JsonPropertyName("reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RealtimeReasoningConfig? Reasoning { get; set; }
}

public class RealtimeSessionTurnDetection
{
    [JsonPropertyName("interrupt_response")]
    public bool InterruptResponse { get; set; } = true;

    [JsonPropertyName("create_response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CreateResponse { get; set; }

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

    /// <summary>
    /// For semantic_vad
    /// </summary>
    [JsonPropertyName("eagerness")]
    public string Eagerness { get;set; } = "auto";
}

public class InputAudioTranscription
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = Gpt4xModelConstants.GPT_4o_Transcribe;

    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prompt { get; set; }

    [JsonPropertyName("delay")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Delay { get; set; }
}

public class InputAudioNoiseReduction
{
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Type { get; set; } = "far_field";
}

public class RealtimeAudioConfig
{
    [JsonPropertyName("input")]
    public RealtimeInputAudioConfig Input { get; set; } = new();

    [JsonPropertyName("output")]
    public RealtimeOutputAudioConfig Output { get; set; } = new();
}

public class RealtimeInputAudioConfig
{
    [JsonPropertyName("format")]
    public RealtimeAudioFormat Format { get; set; } = new();

    [JsonPropertyName("noise_reduction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InputAudioNoiseReduction? NoiseReduction { get; set; }

    [JsonPropertyName("transcription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InputAudioTranscription? Transcription { get; set; }

    [JsonPropertyName("turn_detection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RealtimeSessionTurnDetection? TurnDetection { get; set; }
}

public class RealtimeOutputAudioConfig
{
    [JsonPropertyName("format")]
    public RealtimeAudioFormat Format { get; set; } = new();

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "alloy";

    [JsonPropertyName("speed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Speed { get; set; }
}

public class RealtimeAudioFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rate { get; set; }
}

public class RealtimeReasoningConfig
{
    /// <summary>
    /// "minimal", "low", "medium", "high", "xhigh" 
    /// </summary>
    [JsonPropertyName("effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Effort { get; set; }
}