namespace BotSharp.Abstraction.Files;

public interface IBotSharpFileService
{
    string GetDirectory(string conversationId);
    IEnumerable<MessageFileModel> GetChatImages(string conversationId, List<RoleDialogModel> conversations, int? offset = null);
    IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, bool imageOnly = false);
    string GetMessageFile(string conversationId, string messageId, string fileName);
    bool HasConversationFiles(string conversationId);
    Task<bool> SaveMessageFiles(string conversationId, string messageId, List<BotSharpFile> files);

    string GetUserAvatar();
    bool SaveUserAvatar(BotSharpFile file);

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

    /// <summary>
    /// Get file bytes and content type from data, e.g., "data:image/png;base64,aaaaaaaaa"
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    (string, byte[]) GetFileInfoFromData(string data);
}
