namespace BotSharp.Plugin.OpenAI.Models.Live;

/// <summary>
/// Event type strings sent by the client to the Live endpoint.
/// Reference to https://developers.openai.com/api/docs/guides/voice-websockets
/// </summary>
public static class LiveClientEventType
{
    public const string SessionStart = "session.start";
    public const string SessionUpdate = "session.update";
    public const string SessionClose = "session.close";
    public const string InputAudioAppend = "session.input_audio.append";
    public const string InputAudioMute = "session.input_audio.mute";
    public const string InputAudioUnmute = "session.input_audio.unmute";

    /// <summary>
    /// Trusted behavior instructions injected into the live conversation.
    /// </summary>
    public const string InstructionsAppend = "session.instructions.append";

    /// <summary>
    /// Factual context the model may use but does not speak immediately.
    /// </summary>
    public const string ThinkingAppend = "session.thinking.append";

    /// <summary>
    /// Content the model paraphrases and speaks aloud.
    /// </summary>
    public const string CommentaryAppend = "session.commentary.append";

    /// <summary>
    /// Appends an item (function_call_output or typed message) to the backend Responses turn.
    /// </summary>
    public const string ResponseItemCreate = "response.item.create";

    /// <summary>
    /// Asks the backend handler to continue once all required tool results are submitted.
    /// </summary>
    public const string ResponseCreate = "response.create";
}

/// <summary>
/// Event type strings received from the Live endpoint.
/// </summary>
public static class LiveServerEventType
{
    public const string SessionStarted = "session.started";
    public const string SessionUpdated = "session.updated";
    public const string SessionClosed = "session.closed";
    public const string UsageUpdated = "session.usage.updated";
    public const string Error = "error";

    public const string OutputAudioDelta = "session.output_audio.delta";
    public const string InputTranscriptDelta = "session.input_transcript.delta";
    public const string OutputTranscriptDelta = "session.output_transcript.delta";

    public const string DelegationCreated = "session.delegation.created";

    public const string InputAudioMuted = "session.input_audio.muted";
    public const string InputAudioUnmuted = "session.input_audio.unmuted";

    public const string InstructionsAppended = "session.instructions.appended";
    public const string ThinkingAppended = "session.thinking.appended";
    public const string CommentaryAppended = "session.commentary.appended";

    /// <summary>
    /// Envelope carrying a nested Responses API event from the backend handler.
    /// </summary>
    public const string ResponseEvent = "response.event";
}

/// <summary>
/// Nested event types carried inside a <see cref="LiveServerEventType.ResponseEvent"/> envelope.
/// </summary>
public static class LiveResponseInnerEventType
{
    public const string OutputItemDone = "response.output_item.done";
    public const string OutputTextDelta = "response.output_text.delta";
    public const string Completed = "response.completed";
    public const string Failed = "response.failed";
    public const string Incomplete = "response.incomplete";
}

public static class LiveDelegationType
{
    /// <summary>
    /// Live calls the backend model itself. The only mode this provider supports; "client",
    /// where the application answers delegations on its own, is not implemented.
    /// </summary>
    public const string Responses = "responses";
}
