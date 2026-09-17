namespace BotSharp.Plugin.OpenAI.Settings;

/// <summary>
/// Defaults for the OpenAI Live endpoint (gpt-live-1). Agent level realtime config and
/// conversation state take precedence over these values at runtime.
/// </summary>
public class LiveSettings
{
    /// <summary>
    /// Voice model that owns the conversation.
    /// </summary>
    public string Model { get; set; } = LiveModelConstants.GPT_Live_1;

    /// <summary>
    /// Live only accepts its own voice set; realtime voice names such as "alloy" are rejected.
    /// </summary>
    public string Voice { get; set; } = "marin";

    /// <summary>
    /// Prompt for the voice model: how the conversation sounds and when to delegate.
    /// The agent instruction is NOT used here - it goes to the backend handler instead.
    /// Falls back to <see cref="LivePromptConstants.DefaultVoiceInstruction"/>.
    /// </summary>
    public string? VoiceInstructions { get; set; }

    /// <summary>
    /// Whether the model opens the call rather than waiting to be spoken to.
    /// </summary>
    public bool GreetOnStart { get; set; } = true;

    /// <summary>
    /// Fallback greeting for agents with no ".welcome" template of their own; an agent that has
    /// one greets with that instead, so it keeps the opening line it already uses in text chat.
    /// Either way the model paraphrases it rather than reading it out word for word, which is
    /// what keeps a spoken greeting sounding natural.
    /// </summary>
    public string? Greeting { get; set; } = LivePromptConstants.DefaultGreeting;

    /// <summary>
    /// Backend reasoning model that does the thinking and runs the tools behind the
    /// conversation. Live calls it itself and feeds the results back into the call.
    /// </summary>
    public string BackendModel { get; set; } = "gpt-5.6-luna";

    /// <summary>
    /// auto, default, flex, or priority (also accepted as "fast").
    /// Applies to the backend Responses call, not to the billed voice minutes.
    /// </summary>
    public string? ServiceTier { get; set; } = "auto";

    public bool? ParallelToolCalls { get; set; }

    /// <summary>
    /// Records the session so it can be downloaded or forked later.
    /// </summary>
    public bool Store { get; set; }

    /// <summary>
    /// A turn is normally closed by the other speaker starting, or by the model's audio going
    /// idle, since Live emits no completion marker. This is the backstop for the model's last
    /// turn: armed by its transcript deltas alone, so a pause longer than this mid reply will
    /// split it into two messages.
    /// </summary>
    public int TranscriptIdleMs { get; set; } = 500;

    /// <summary>
    /// Backstop for the user's last turn. Longer than <see cref="TranscriptIdleMs"/> because
    /// nothing arms the buffer between words: at 500ms a speaker pausing mid-sentence would be
    /// filed as a finished turn before the model ever got to reply and close it properly.
    /// </summary>
    public int InputTranscriptIdleMs { get; set; } = 4000;

    /// <summary>
    /// Idle gap after which the model audio stream is treated as finished.
    /// </summary>
    public int AudioIdleMs { get; set; } = 1000;

    /// <summary>
    /// Budget for a single session.*.append. The server caps each append at 500 tokens and
    /// rejects anything larger, so this leaves headroom for the estimate being approximate.
    /// Spoken content over the budget is split across appends; a directive or a tool result is
    /// trimmed instead, because splitting those would change what they mean.
    /// </summary>
    public int MaxAppendTokens { get; set; } = 450;

}
