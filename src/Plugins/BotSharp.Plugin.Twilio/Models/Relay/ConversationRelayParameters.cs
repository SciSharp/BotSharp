namespace BotSharp.Plugin.Twilio.Models.Relay;

/// <summary>
/// Custom TwiML Parameter names passed into the ConversationRelay "setup" message
/// </summary>
public static class ConversationRelayParameters
{
    public const string WelcomeGreetingLength = "welcome-greeting-length";
}

/// <summary>
/// Values we put in the "end" message's handoffData, read back by the Connect action callback
/// </summary>
public static class ConversationRelayHandoff
{
    public const string HangUp = "hangup";
    public const string Transfer = "transfer";
}
