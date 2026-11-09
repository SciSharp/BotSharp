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
    /// Seconds the session-ended callback for a caller hanging up waits for the turn still running on this instance to
    /// save the conversation before the hooks are told. Keep it under Twilio's 15-second webhook timeout; a session on
    /// another instance is not waited for.
    /// </summary>
    public double CallerHangUpWaitSeconds { get; set; } = 10;

    /// <summary>
    /// Verify the X-Twilio-Signature header of the WebSocket handshake. Needs Twilio:RequestValidation:AuthToken;
    /// without it every handshake is refused
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

    /// <summary>
    /// Seconds after the caller cut in, with nothing from them since, before the line they cut into is said again, once
    /// (a cut-in no words follow is a noise or the line's own echo). A line said again and cut into again is not said a
    /// third time: the caller is talking. 0 turns it off.
    /// </summary>
    public double ResumeAfterCutInSeconds { get; set; }

    /// <summary>
    /// Seconds of caller silence before each idle prompt, in turn; the last one is before hanging up. Empty uses
    /// <see cref="IdleTimeoutSeconds"/> for every one.
    /// </summary>
    public List<double> IdleTimeouts { get; set; } = [];

    /// <summary>
    /// The first idle prompt asks the question the agent's last line asked again ("Sorry, I didn't catch that. ..."), the
    /// next ones ask if the caller is still there, and the hang-up says goodbye (Twilio:PromptTexts say-it-again-question,
    /// still-there and idle-goodbye). Off, every idle prompt is a "say it again" one and the hang-up is silent.
    /// </summary>
    public bool IdleAsksAgain { get; set; }

    /// <summary>
    /// Drop a caller's words that came in before they could hear the line just said (TooSoonToAnswer) when they only
    /// acknowledge ("I agree.", "Okay.") and that line asks something: the tail of what they said before, which the
    /// transcription split off. Words said after the caller heard a few words of the line are kept.
    /// </summary>
    public bool DropBackchannelTails { get; set; }

    /// <summary>
    /// Settings for one agent's calls, by agent id, over the ones above
    /// </summary>
    public Dictionary<string, ConversationRelayAgentSetting> Agents { get; set; } = [];

    /// <summary>
    /// The settings for an agent's calls: these, with what <see cref="Agents"/> sets for it in their place
    /// </summary>
    public ConversationRelaySetting For(string? agentId)
    {
        if (agentId == null || !Agents.TryGetValue(agentId, out var agent))
        {
            return this;
        }

        var settings = (ConversationRelaySetting)MemberwiseClone();
        settings.TranscriptionProvider = agent.TranscriptionProvider ?? TranscriptionProvider;
        settings.SpeechModel = agent.SpeechModel ?? SpeechModel;
        settings.InterruptSensitivity = agent.InterruptSensitivity ?? InterruptSensitivity;
        settings.IgnoreBackchannel = agent.IgnoreBackchannel ?? IgnoreBackchannel;
        settings.IdleTimeoutSeconds = agent.IdleTimeoutSeconds ?? IdleTimeoutSeconds;
        settings.MaxIdlePrompts = agent.MaxIdlePrompts ?? MaxIdlePrompts;
        settings.IdleTimeouts = agent.IdleTimeouts ?? IdleTimeouts;
        settings.IdleAsksAgain = agent.IdleAsksAgain ?? IdleAsksAgain;
        settings.ResumeAfterCutInSeconds = agent.ResumeAfterCutInSeconds ?? ResumeAfterCutInSeconds;
        settings.DropBackchannelTails = agent.DropBackchannelTails ?? DropBackchannelTails;
        return settings;
    }

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
