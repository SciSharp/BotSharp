using System.IO;

namespace BotSharp.Abstraction.Files;

public interface IFileBasicService
{
    #region Conversation
    /// <summary>
    /// Get the files that have been uploaded in the chat.
    /// If includeScreenShot is true, it will take the screenshots of non-image files, such as pdf, and return the screenshots instead of the original file.
    /// </summary>
    /// <param name="conversationId"></param>
    /// <param name="source"></param>
    /// <param name="conversations"></param>
    /// <param name="contentTypes"></param>
    /// <param name="includeScreenShot"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    Task<IEnumerable<MessageFileModel>> GetChatFiles(string conversationId, string source,
        IEnumerable<RoleDialogModel> conversations, IEnumerable<string> contentTypes,
        bool includeScreenShot = false, int? offset = null);

    /// <summary>
    /// Get the files that have been uploaded in the chat. No screenshot images are included.
    /// </summary>
    /// <param name="conversationId"></param>
    /// <param name="messageIds"></param>
    /// <param name="source"></param>
    /// <param name="imageOnly"></param>
    /// <returns></returns>
    IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, string source, IEnumerable<string>? contentTypes = null);
    string GetMessageFile(string conversationId, string messageId, string source, string index, string fileName);
    IEnumerable<MessageFileModel> GetMessagesWithFile(string conversationId, IEnumerable<string> messageIds);
    bool SaveMessageFiles(string conversationId, string messageId, string source, List<BotSharpFile> files);

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
    #endregion

    #region User
    string GetUserAvatar();
    bool SaveUserAvatar(BotSharpFile file);
    #endregion

    #region Common
    string GetDirectory(string conversationId);
    byte[] GetFileBytes(string fileStorageUrl);
    bool SaveFileStreamToPath(string filePath, Stream stream);
    bool SaveFileBytesToPath(string filePath, byte[] bytes);
    string GetParentDir(string dir, int level = 1);
    bool ExistDirectory(string? dir);
    void CreateDirectory(string dir);
    void DeleteDirectory(string dir);
    string BuildDirectory(params string[] segments);
    #endregion
}
