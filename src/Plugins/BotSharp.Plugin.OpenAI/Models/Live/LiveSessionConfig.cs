using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Plugin.OpenAI.Models.Live;

/// <summary>
/// Session object sent with session.start, and partially with session.update.
/// Reference to https://developers.openai.com/api/docs/guides/live
/// </summary>
public class LiveSessionConfig
{
    /// <summary>
    /// Required at session.start, immutable afterwards.
    /// </summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    /// <summary>
    /// Conversation style guidance, up to 16,384 tokens. Detailed workflows belong in
    /// <see cref="LiveResponsesDelegationConfig.Instructions"/> instead.
    /// </summary>
    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    /// <summary>
    /// Prior conversation turns to seed the session with.
    /// </summary>
    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveSessionInputItem[]? Input { get; set; }

    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveAudioConfig? Audio { get; set; }

    [JsonPropertyName("delegation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveDelegationConfig? Delegation { get; set; }

    /// <summary>
    /// Enables session recording so it can be downloaded or forked later.
    /// </summary>
    [JsonPropertyName("store")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Store { get; set; }
}

public class LiveSessionInputItem
{
    /// <summary>
    /// developer, user or assistant.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}

public class LiveAudioConfig
{
    /// <summary>
    /// Wire format of the audio carried over the WebSocket. Negotiated automatically on WebRTC.
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveAudioFormat? Format { get; set; }

    [JsonPropertyName("output")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveOutputAudioConfig? Output { get; set; }
}

/// <summary>
/// audio/pcm at 24000 or 16000, audio/pcmu or audio/pcma at 8000.
/// </summary>
public class LiveAudioFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "audio/pcm";

    [JsonPropertyName("rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rate { get; set; }
}

public class LiveOutputAudioConfig
{
    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Voice { get; set; }
}

/// <summary>
/// Decides who runs the reasoning and tool calls behind the voice conversation.
/// The mode cannot be changed on a live session; start a new one instead.
/// </summary>
public class LiveDelegationConfig
{
    /// <summary>
    /// responses or client.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = LiveDelegationType.Responses;

    [JsonPropertyName("responses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveResponsesDelegationConfig? Responses { get; set; }
}

/// <summary>
/// Backend handler configuration used when delegation type is "responses".
/// This is the only part of the session that session.update may change.
/// </summary>
public class LiveResponsesDelegationConfig
{
    /// <summary>
    /// Backend reasoning model, required when the delegation is created.
    /// </summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionDef[]? Tools { get; set; }

    /// <summary>
    /// auto, required, none or a named function.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolChoice { get; set; }

    [JsonPropertyName("parallel_tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ParallelToolCalls { get; set; }

    /// <summary>
    /// Minimum 16 when set.
    /// </summary>
    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// auto, default, flex, or priority (also accepted as "fast").
    /// Applies to the backend Responses call, not to the billed voice minutes.
    /// </summary>
    [JsonPropertyName("service_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceTier { get; set; }

    [JsonPropertyName("reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LiveReasoningConfig? Reasoning { get; set; }
}

public class LiveReasoningConfig
{
    /// <summary>
    /// minimal, low, medium, high or xhigh.
    /// </summary>
    [JsonPropertyName("effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Effort { get; set; }
}
