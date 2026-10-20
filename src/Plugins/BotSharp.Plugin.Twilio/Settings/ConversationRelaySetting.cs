namespace BotSharp.Plugin.Twilio.Settings;

/// <summary>
/// Twilio ConversationRelay: Twilio transcribes the caller and speaks our replies, exchanging text with us over a WebSocket.
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay
/// </summary>
public class ConversationRelaySetting
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Agents that use ConversationRelay for outbound calls. Empty means every agent.
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
    /// How many times to reconnect a session that failed (e.g. the pod holding the WebSocket went away)
    /// </summary>
    public int MaxReconnects { get; set; } = 2;

    /// <summary>
    /// Verify the X-Twilio-Signature header of the WebSocket handshake
    /// </summary>
    public bool ValidateSignature { get; set; } = true;

    public bool IsEnabledFor(string? agentId)
    {
        return Enabled && (AgentIds.Count == 0 || (agentId != null && AgentIds.Contains(agentId)));
    }
}
