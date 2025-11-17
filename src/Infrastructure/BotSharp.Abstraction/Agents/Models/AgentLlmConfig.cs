namespace BotSharp.Abstraction.Agents.Models;

public class AgentLlmConfig
{
    /// <summary>
    /// Is inherited from default Agent Settings
    /// </summary>
    [JsonPropertyName("is_inherit")]
    public bool IsInherit { get; set; }

    /// <summary>
    /// Completion Provider
    /// </summary>
    [JsonPropertyName("provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Provider { get; set; }

    /// <summary>
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    /// <summary>
    /// Max recursion depth
    /// </summary>
    [JsonPropertyName("max_recursion_depth")]
    public int MaxRecursionDepth { get; set; } = 3;

    /// <summary>
    /// Max output token
    /// </summary>
    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Reasoning effort level
    /// </summary>
    [JsonPropertyName("reasoning_effort_level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffortLevel { get; set; }

    /// <summary>
    /// Image composition config
    /// </summary>
    [JsonPropertyName("image_composition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmImageCompositionConfig? ImageComposition { get; set; }

    /// <summary>
    /// Audio transcription config
    /// </summary>
    [JsonPropertyName("audio_transcription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmAudioTranscriptionConfig? AudioTranscription { get; set; }

    /// <summary>
    /// Realtime config
    /// </summary>
    [JsonPropertyName("realtime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmRealtimeConfig? Realtime { get; set; }
}

public class LlmImageCompositionConfig : LlmProviderModel
{
}

public class LlmAudioTranscriptionConfig : LlmProviderModel
{
}

public class LlmRealtimeConfig : LlmProviderModel
{
}