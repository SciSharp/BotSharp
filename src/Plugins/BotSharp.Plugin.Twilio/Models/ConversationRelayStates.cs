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
}
