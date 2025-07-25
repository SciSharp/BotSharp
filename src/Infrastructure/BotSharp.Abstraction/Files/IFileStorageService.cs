using System.IO;

namespace BotSharp.Abstraction.Files;

public interface IFileStorageService
{
    #region Common
    string GetDirectory(string conversationId);
    IEnumerable<string> GetFiles(string relativePath, string? searchQuery = null);
    BinaryData GetFileBytes(string fileStorageUrl);
    bool SaveFileStreamToPath(string filePath, Stream stream);
    bool SaveFileBytesToPath(string filePath, BinaryData binary);
    string GetParentDir(string dir, int level = 1);
    bool ExistDirectory(string? dir);
    void CreateDirectory(string dir);
    void DeleteDirectory(string dir);
    string BuildDirectory(params string[] segments);
    #endregion

    #region Conversation
    /// <summary>
    /// Get the message file screenshots for specific content types, e.g., pdf
    /// </summary>
    /// <param name="conversationId"></param>
    /// <param name="messageIds"></param>
    /// <returns></returns>
    Task<IEnumerable<MessageFileModel>> GetMessageFileScreenshotsAsync(string conversationId, IEnumerable<string> messageIds);

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
    bool SaveMessageFiles(string conversationId, string messageId, string source, List<FileDataModel> files);

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
    bool SaveUserAvatar(FileDataModel file);
    #endregion

    #region Speech
    bool SaveSpeechFile(string conversationId, string fileName, BinaryData data);
    BinaryData GetSpeechFile(string conversationId, string fileName);
    #endregion

    #region Knowledge
    bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, Guid fileId, string fileName, BinaryData fileData);

    /// <summary>
    /// Delete files in a knowledge collection, given the vector store provider. If "fileId" is null, delete all files in the collection.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="vectorStoreProvider"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    bool DeleteKnowledgeFile(string collectionName, string vectorStoreProvider, Guid? fileId = null);
    string GetKnowledgeBaseFileUrl(string collectionName, string vectorStoreProvider, Guid fileId, string fileName);
    BinaryData GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, Guid fileId, string fileName);
    #endregion
}
