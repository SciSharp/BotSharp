using System.IO;

namespace BotSharp.Core.Files.Services
{
    public partial class BotSharpFileService
    {
        public async Task SaveSpeechFileAsync(string conversationId, string fileName, BinaryData data)
        {
            var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, TEXT_TO_SPEECH_FOLDER, conversationId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using var file = File.Create(Path.Combine(dir, fileName));
            using var input = data.ToStream();
            await input.CopyToAsync(file);
        }

        public async Task<BinaryData> RetrieveSpeechFileAsync(string conversationId, string fileName)
        {
            var path = Path.Combine(_baseDir, CONVERSATION_FOLDER, TEXT_TO_SPEECH_FOLDER, conversationId,  fileName);
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
            return await BinaryData.FromStreamAsync(file);
        }
    }
}
