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
}
