namespace BotSharp.Abstraction.Messaging;

public interface IRichMessage
{
    string Type { get; }
    string Text { get; set; }
}
