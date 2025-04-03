using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;

namespace BotSharp.Plugin.Google.Core
{
    public class NullFileStorageService:IFileStorageService
    {
        public string GetDirectory(string conversationId)
        {
            return $"FakeDirectory/{conversationId}";
        }

        public IEnumerable<string> GetFiles(string relativePath, string? searchQuery = null)
        {
            return new List<string> { "FakeFile1.txt", "FakeFile2.txt" };
        }

        public byte[] GetFileBytes(string fileStorageUrl)
        {
            return new byte[] { 0x00, 0x01, 0x02 };
        }

        public bool SaveFileStreamToPath(string filePath, Stream stream)
        {
            return true;
        }

        public bool SaveFileBytesToPath(string filePath, byte[] bytes)
        {
            return true;
        }

        public string GetParentDir(string dir, int level = 1)
        {
            return "FakeParentDirectory";
        }

        public bool ExistDirectory(string? dir)
        {
            return true;
        }

        public void CreateDirectory(string dir)
        {
        }

        public void DeleteDirectory(string dir)
        {
        }

        public string BuildDirectory(params string[] segments)
        {
            return string.Join("/", segments);
        }

        public Task<IEnumerable<MessageFileModel>> GetMessageFileScreenshotsAsync(string conversationId, IEnumerable<string> messageIds)
        {
            return Task.FromResult<IEnumerable<MessageFileModel>>(new List<MessageFileModel>
            {
                new MessageFileModel { FileName = "Screenshot1.png" },
                new MessageFileModel { FileName = "Screenshot2.png" }
            });
        }

        public IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, string source,
            IEnumerable<string>? contentTypes = null)
        {
            return new List<MessageFileModel>
            {
                new MessageFileModel { FileName = "File1.docx" },
                new MessageFileModel { FileName = "File2.pdf" }
            };
        }

        public string GetMessageFile(string conversationId, string messageId, string source, string index, string fileName)
        {
            return $"FakePath/{fileName}";
        }

        public IEnumerable<MessageFileModel> GetMessagesWithFile(string conversationId, IEnumerable<string> messageIds)
        {
            return new List<MessageFileModel>
            {
                new MessageFileModel { FileName = "MessageFile1.jpg" },
                new MessageFileModel { FileName = "MessageFile2.png" }
            };
        }

        public bool SaveMessageFiles(string conversationId, string messageId, string source, List<FileDataModel> files)
        {
            return true;
        }

        public bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId,
            string? newMessageId = null)
        {
            return true;
        }

        public bool DeleteConversationFiles(IEnumerable<string> conversationIds)
        {
            return true;
        }

        public string GetUserAvatar()
        {
            return "FakeUserAvatar.png";
        }

        public bool SaveUserAvatar(FileDataModel file)
        {
            return true;
        }

        public bool SaveSpeechFile(string conversationId, string fileName, BinaryData data)
        {
            return true;
        }

        public BinaryData GetSpeechFile(string conversationId, string fileName)
        {
            return BinaryData.FromBytes(new byte[] { 0x03, 0x04, 0x05 });
        }

        public bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, Guid fileId, string fileName,
            BinaryData fileData)
        {
            return true;
        }

        public bool DeleteKnowledgeFile(string collectionName, string vectorStoreProvider, Guid? fileId = null)
        {
            return true;
        }

        public string GetKnowledgeBaseFileUrl(string collectionName, string vectorStoreProvider, Guid fileId, string fileName)
        {
            return $"https://fakeurl.com/{fileName}";
        }

        public BinaryData GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, Guid fileId,
            string fileName)
        {
            return BinaryData.FromBytes(new byte[] { 0x06, 0x07, 0x08 });
        }
    }
}