using System.IO;

namespace BotSharp.Core.Files.Services
{
    public partial class LocalFileStorageService
    {
        public async Task SaveSpeechFileAsync(string conversationId, string fileName, BinaryData data)
        {
            var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, TEXT_TO_SPEECH_FOLDER);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath)) return;
            using var file = File.Create(filePath);
            using var input = data.ToStream();
            await input.CopyToAsync(file);
        }

        public async Task<BinaryData> RetrieveSpeechFileAsync(string conversationId, string fileName)
        {
            var path = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, TEXT_TO_SPEECH_FOLDER,  fileName);
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
            return await BinaryData.FromStreamAsync(file);
        }
    }
}
