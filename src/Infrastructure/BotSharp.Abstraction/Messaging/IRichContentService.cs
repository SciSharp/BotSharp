namespace BotSharp.Abstraction.Messaging;

public interface IRichContentService
{
    List<IRichMessage> ConvertToMessages(string content);
}
