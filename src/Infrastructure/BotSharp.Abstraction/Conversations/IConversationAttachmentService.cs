namespace BotSharp.Abstraction.Conversations;

public interface IConversationAttachmentService
{
    string GetDirectory(string conversationId);
    IEnumerable<OutputFileModel> GetConversationFiles(string conversationId, string messageId);
    string? GetMessageFile(string conversationId, string messageId, string fileType, int index);
    void SaveConversationFiles(List<BotSharpFile> files);
}
