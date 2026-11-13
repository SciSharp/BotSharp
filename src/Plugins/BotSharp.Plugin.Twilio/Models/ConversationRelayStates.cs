namespace BotSharp.Plugin.Twilio.Models;

/// <summary>
/// Conversation states a phone flow sets to steer the ConversationRelay session
/// </summary>
public static class ConversationRelayStates
{
    /// <summary>
    /// "true" while what the agent says must play to the end, whoever speaks over it: a flow sets it around a line the
    /// caller has to hear, such as a confirmation number, and clears it once that line is said
    /// </summary>
    public const string KeepUninterrupted = "twilio_relay_keep_uninterrupted";

    /// <summary>
    /// "true" for a turn whose caller missed some of what the agent said once the turn before was over (text a tool gave,
    /// spoken after the turn): they cut into it, or what the turn answers was said before it went out. The history keeps
    /// that text whole, so this is the only sign. Set by the session at the start of every turn.
    /// </summary>
    public const string LastReplyCutOff = "twilio_relay_last_reply_cut_off";

    /// <summary>
    /// "true" while the caller is busy with something the agent asked them to do on their phone, such as filling in a form
    /// it texted them, and the agent waits quietly: silence is expected, so instead of the idle prompts the caller hears a
    /// soft check-in (prompt quiet-wait-check-in) each QuietWaitSeconds, and the call is hung up only after
    /// MaxQuietWaitPrompts of them. Read when a turn's reply is in; a flow clears it once the wait is over.
    /// </summary>
    public const string QuietWait = "twilio_relay_quiet_wait";

    /// <summary>
    /// What is said as the session hangs up on a caller who stayed quiet, in place of the idle-goodbye prompt: a flow sets
    /// it while something is still to be told, such as the confirmation number of a request already made, and clears it
    /// once that is said. Read when a turn's reply is in.
    /// </summary>
    public const string GoodbyeOnSilence = "twilio_relay_goodbye_on_silence";
}
