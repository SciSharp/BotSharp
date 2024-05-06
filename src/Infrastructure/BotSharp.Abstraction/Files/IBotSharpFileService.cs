namespace BotSharp.Abstraction.Files;

public interface IBotSharpFileService
{
    string GetDirectory(string conversationId);
    IEnumerable<OutputFileModel> GetConversationFiles(string conversationId, string messageId);
    string? GetMessageFile(string conversationId, string messageId, string fileName, string fileType);
    void SaveConversationFiles(string conversationId, List<BotSharpFile> files);
}
