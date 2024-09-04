using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public bool SaveSpeechFile(string conversationId, string fileName, BinaryData data)
    {
        try
        {
            var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, TEXT_TO_SPEECH_FOLDER);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath)) return false;

            using var fs = File.Create(filePath);
            using var ds = data.ToStream();
            ds.CopyTo(fs);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving speech file. {fileName} ({conversationId})\r\n{ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public BinaryData GetSpeechFile(string conversationId, string fileName)
    {
        var path = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, TEXT_TO_SPEECH_FOLDER,  fileName);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
        return BinaryData.FromStream(file);
    }
}
