namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public bool SaveSpeechFile(string conversationId, string fileName, BinaryData data)
    {
        try
        {
            var file = $"{CONVERSATION_FOLDER}/{conversationId}/{TEXT_TO_SPEECH_FOLDER}/{fileName}";
            var exist = _cosClient.BucketClient.DoesObjectExist(file);
            if (exist)
            {
                return false;
            }

            return _cosClient.BucketClient.UploadBytes(file, data.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving speech file. {fileName} ({conversationId})\r\n{ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public BinaryData GetSpeechFile(string conversationId, string fileName)
    {
        var dir = $"{CONVERSATION_FOLDER}/{conversationId}/{TEXT_TO_SPEECH_FOLDER}";
        var key = $"{dir}/{fileName}";
        var file = _cosClient.BucketClient.GetDirFile(dir, key);

        if (string.IsNullOrWhiteSpace(file))
        {
            return BinaryData.Empty;
        }

        var bytes = _cosClient.BucketClient.DownloadFileBytes(file);
        return BinaryData.FromBytes(bytes);
    }
}
