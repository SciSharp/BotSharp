namespace BotSharp.Plugin.Twilio.Settings;

/// <summary>
/// Twilio ConversationRelay: Twilio transcribes the caller and speaks our replies, exchanging text with us over a WebSocket.
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay
/// </summary>
public class ConversationRelaySetting
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Agents that use ConversationRelay for their calls, outbound and inbound. Empty means every agent.
    /// </summary>
    public List<string> AgentIds { get; set; } = [];

    public string Language { get; set; } = "en-US";
    public string? TtsProvider { get; set; }
    public string? Voice { get; set; }
    public string? TranscriptionProvider { get; set; }
    public string? SpeechModel { get; set; }
    public string? Hints { get; set; }

    /// <summary>
    /// none, dtmf, speech or any
    /// </summary>
    public string WelcomeGreetingInterruptible { get; set; } = "none";

    /// <summary>
    /// none, dtmf, speech or any
    /// </summary>
    public string Interruptible { get; set; } = "any";

    /// <summary>
    /// low, medium or high
    /// </summary>
    public string InterruptSensitivity { get; set; } = "medium";

    public bool IgnoreBackchannel { get; set; } = true;
    public bool DtmfDetection { get; set; } = true;

    /// <summary>
    /// Seconds of caller silence, after the reply is expected to finish playing, before asking the caller to repeat
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 8;

    /// <summary>
    /// How many times to ask the caller to repeat before hanging up
    /// </summary>
    public int MaxIdlePrompts { get; set; } = 2;

    /// <summary>
    /// How long to wait for the agent's reply before apologizing and hanging up
    /// </summary>
    public int AiResponseTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Send the reply to Twilio while the model is still writing it, so speaking starts with the first words.
    /// Needs a chat provider that supports streaming, and only applies to English conversations.
    /// </summary>
    public bool StreamReplies { get; set; }

    /// <summary>
    /// How many times to reconnect a session that failed (e.g. the pod holding the WebSocket went away)
    /// </summary>
    public int MaxReconnects { get; set; } = 2;

    /// <summary>
    /// Verify the X-Twilio-Signature header of the WebSocket handshake
    /// </summary>
    public bool ValidateSignature { get; set; } = true;

    /// <summary>
    /// Seconds of silence on the line, while the agent works on its reply, before each filler such as "One moment, please.":
    /// the first counts from when the caller finished, each next one from when the previous filler (or anything else
    /// we said) finished. The last value repeats until the reply comes. Empty turns the watchdog off.
    /// </summary>
    public List<double> ResponseWatchdogIntervals { get; set; } = [];

    /// <summary>
    /// Agents the response watchdog applies to. Empty means every ConversationRelay agent.
    /// </summary>
    public List<string> ResponseWatchdogAgentIds { get; set; } = [];

    /// <summary>
    /// Names of Twilio:PromptTexts entries the first filler of a turn is picked from. Empty uses hold-on-1 to hold-on-5.
    /// </summary>
    public List<string> ResponseWatchdogPrompts { get; set; } = [];

    /// <summary>
    /// Names of Twilio:PromptTexts entries the later fillers of a long wait are picked from. Empty uses hold-on-long-1 to hold-on-long-10.
    /// </summary>
    public List<string> ResponseWatchdogLongPrompts { get; set; } = [];

    public bool IsEnabledFor(string? agentId)
    {
        return Enabled && (AgentIds.Count == 0 || (agentId != null && AgentIds.Contains(agentId)));
    }

    public bool IsResponseWatchdogEnabledFor(string? agentId)
    {
        return ResponseWatchdogIntervals.Count > 0 && ResponseWatchdogIntervals.All(seconds => seconds > 0)
            && (ResponseWatchdogAgentIds.Count == 0 || (agentId != null && ResponseWatchdogAgentIds.Contains(agentId)));
    }
}
