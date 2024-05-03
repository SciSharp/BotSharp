namespace BotSharp.Abstraction.Conversations;

public interface IConversationAttachmentService
{
    string GetDirectory(string conversationId);
    void SaveConversationFiles(List<BotSharpFile> files);
}
