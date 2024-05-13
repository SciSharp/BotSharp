namespace BotSharp.Abstraction.Files;

public interface IBotSharpFileService
{
    string GetDirectory(string conversationId);
    IEnumerable<MessageFileModel> GetChatImages(string conversationId, List<RoleDialogModel> conversations, int offset = 2);
    IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, bool imageOnly = false);
    string? GetMessageFile(string conversationId, string messageId, string fileName);
    void SaveMessageFiles(string conversationId, string messageId, List<BotSharpFile> files);

    /// <summary>
    /// Delete files under messages
    /// </summary>
    /// <param name="conversationId">Conversation Id</param>
    /// <param name="messageIds">Files in these messages will be deleted</param>
    /// <param name="targetMessageId">The starting message to delete</param>
    /// <param name="newMessageId">If not null, delete messages while input a new message; otherwise, delete messages only</param>
    /// <returns></returns>
    bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId, string? newMessageId = null);
    bool DeleteConversationFiles(IEnumerable<string> conversationIds);
}
