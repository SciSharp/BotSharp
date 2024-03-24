namespace BotSharp.Abstraction.Conversations.Settings;

public class RateLimitSetting
{
    public int MaxConversationPerDay { get; set; } = 100;
    public int MaxInputLengthPerRequest { get; set; } = 256;
    public int MinTimeSecondsBetweenMessages { get; set; } = 2;
}
