namespace BotSharp.Abstraction.Messaging;

public interface IRichMessage
{
    string Text { get; set; }
    string RichType => "text";
}
