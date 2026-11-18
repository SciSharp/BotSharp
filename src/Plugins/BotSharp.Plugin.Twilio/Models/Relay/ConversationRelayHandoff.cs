namespace BotSharp.Plugin.Twilio.Models.Relay;

/// <summary>
/// Values we put in the "end" message's handoffData, read back by the Connect action callback
/// </summary>
public static class ConversationRelayHandoff
{
    public const string HangUp = "hangup";
    public const string Transfer = "transfer";

    /// <summary>
    /// The session's playback was stuck: the call is connected to a new session, which says what never finished playing
    /// (ConversationRelaySetting.StuckPlaybackSeconds). The handoffData carries it under <see cref="ResumeField"/>.
    /// </summary>
    public const string Reconnect = "reconnect";

    /// <summary>
    /// The handoffData field a <see cref="Reconnect"/> carries its <see cref="ConversationRelayResume"/> in
    /// </summary>
    public const string ResumeField = "resume";
}
