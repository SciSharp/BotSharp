namespace BotSharp.Plugin.OpenAI.Models.Live;

/// <summary>
/// Common envelope of every event received from the Live endpoint.
/// </summary>
public class LiveServerEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    /// <summary>
    /// Echoes the event_id of the client event this one answers.
    /// </summary>
    [JsonPropertyName("client_event_id")]
    public string? ClientEventId { get; set; }
}

public class LiveErrorEvent : LiveServerEvent
{
    [JsonPropertyName("error")]
    public LiveErrorBody Body { get; set; } = new();
}

public class LiveErrorBody
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// e.g. immutable_field_update.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public override string ToString() => $"{Type}: {Message} ({Code})";
}

public class LiveSessionStartedEvent : LiveServerEvent
{
    [JsonPropertyName("session")]
    public LiveSessionConfig? Session { get; set; }
}

public class LiveSessionClosedEvent : LiveServerEvent
{
    /// <summary>
    /// close_requested, expired, content, remote_hangup or connection_lost.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("usage")]
    public LiveUsage? Usage { get; set; }
}

public class LiveUsageUpdatedEvent : LiveServerEvent
{
    [JsonPropertyName("usage")]
    public LiveUsage? Usage { get; set; }
}

/// <summary>
/// Live voice sessions are billed by duration rather than tokens.
/// Backend Responses usage is reported separately through the response.event envelope.
/// </summary>
public class LiveUsage
{
    [JsonPropertyName("seconds")]
    public double Seconds { get; set; }

    [JsonPropertyName("context_window")]
    public LiveContextWindowUsage? ContextWindow { get; set; }
}

public class LiveContextWindowUsage
{
    [JsonPropertyName("usage_ratio")]
    public double UsageRatio { get; set; }
}

/// <summary>
/// session.output_audio.delta - a chunk of base64 encoded model audio.
/// </summary>
public class LiveOutputAudioDelta : LiveServerEvent
{
    [JsonPropertyName("delta")]
    public string? Delta { get; set; }

    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }
}

/// <summary>
/// session.input_transcript.delta and session.output_transcript.delta.
/// Live emits no turn completion marker, and intervals may overlap.
/// </summary>
public class LiveTranscriptDelta : LiveServerEvent
{
    [JsonPropertyName("delta")]
    public string? Delta { get; set; }

    [JsonPropertyName("start_ms")]
    public long? StartMs { get; set; }

    [JsonPropertyName("end_ms")]
    public long? EndMs { get; set; }
}

/// <summary>
/// session.delegation.created - raised when delegation type is "client" and the model
/// hands a task to the application backend.
/// </summary>
public class LiveDelegationCreatedEvent : LiveServerEvent
{
    [JsonPropertyName("offset_ms")]
    public long? OffsetMs { get; set; }

    [JsonPropertyName("delegation")]
    public LiveDelegation? Delegation { get; set; }
}

public class LiveDelegation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// client or responses.
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }
}
