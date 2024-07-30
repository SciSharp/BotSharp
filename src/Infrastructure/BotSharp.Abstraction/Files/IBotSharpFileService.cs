using System.IO;

namespace BotSharp.Abstraction.Files;

public interface IBotSharpFileService
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
    IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, string source, bool imageOnly = false);
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

    #region Image
    Task<RoleDialogModel> GenerateImage(string? provider, string? model, string text);
    Task<RoleDialogModel> VaryImage(string? provider, string? model, BotSharpFile image);
    Task<RoleDialogModel> EditImage(string? provider, string? model, string text, BotSharpFile image);
    Task<RoleDialogModel> EditImage(string? provider, string? model, string text, BotSharpFile image, BotSharpFile mask);
    #endregion

    #region Pdf
    /// <summary>
    /// Take screenshots of pdf pages and get response from llm
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="files">Pdf files</param>
    /// <returns></returns>
    Task<string> ReadPdf(string? provider, string? model, string? modelId, string prompt, List<BotSharpFile> files);
    #endregion

    #region User
    string GetUserAvatar();
    bool SaveUserAvatar(BotSharpFile file);
    #endregion

    #region Common
    /// <summary>
    /// Get file bytes and content type from data, e.g., "data:image/png;base64,aaaaaaaaa"
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    (string, byte[]) GetFileInfoFromData(string data);
    string GetDirectory(string conversationId);
    string GetFileContentType(string filePath);
    byte[] GetFileBytes(string fileStorageUrl);
    bool SavefileToPath(string filePath, Stream stream);
    #endregion
}
