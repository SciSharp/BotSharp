namespace BotSharp.Plugin.Twilio.Models.Relay;

/// <summary>
/// What a session connected again in place of one whose playback was stuck starts with: the lines the caller never heard
/// all of, and how the flow last said to treat a silent caller
/// </summary>
/// <remarks>
/// The stuck session keeps it (ConversationRelayResumes), Twilio's callback takes it into the new relay's TwiML as a
/// Parameter, and the new session reads it from its setup, so each may run on another instance.
/// </remarks>
public class ConversationRelayResume
{
    /// <summary>
    /// The lines said since Twilio last said playback stopped, in order, but fillers and idle prompts
    /// </summary>
    public List<ConversationRelayResumeLine> Lines { get; set; } = [];

    /// <summary>
    /// The flow was waiting quietly for the caller (ConversationRelayStates.QuietWait)
    /// </summary>
    public bool QuietWait { get; set; }

    /// <summary>
    /// What the flow said to say if the caller stays silent until the hang-up (ConversationRelayStates.GoodbyeOnSilence)
    /// </summary>
    public string? GoodbyeOnSilence { get; set; }
}

public class ConversationRelayResumeLine
{
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// It went out to play to the end whoever speaks over it, and goes out so again
    /// </summary>
    public bool KeepUninterrupted { get; set; }
}
