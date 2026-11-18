namespace BotSharp.Plugin.Twilio.Models.Relay;

/// <summary>
/// Custom TwiML Parameter names passed into the ConversationRelay "setup" message
/// </summary>
public static class ConversationRelayParameters
{
    public const string WelcomeGreetingLength = "welcome-greeting-length";

    /// <summary>
    /// How many times the call's relay has been connected again
    /// </summary>
    public const string Reconnects = "reconnects";

    /// <summary>
    /// What a session connected again in place of one whose playback was stuck starts with (<see cref="ConversationRelayResume"/>)
    /// </summary>
    public const string Resume = "resume";
}
