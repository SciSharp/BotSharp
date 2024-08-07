namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public string GetDirectory(string conversationId)
    {
        return $"{CONVERSATION_FOLDER}/{conversationId}/attachments/";
    }

    public byte[] GetFileBytes(string fileStorageUrl)
    {
        try
        {
            var fileData = _cosClient.BucketClient.DownloadFileBytes(fileStorageUrl);
            return fileData;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when get file bytes: {ex.Message}\r\n{ex.InnerException}");
        }
        return Array.Empty<byte>();
    }

    public bool SavefileToPath(string filePath, Stream stream)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        try
        {
            return _cosClient.BucketClient.UploadStream(filePath, stream);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving file to path: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public string BuildDirectory(params string[] segments)
    {
        return string.Join("/", segments);
    }

    public void CreateDirectory(string dir)
    {

    }

    public bool ExistDirectory(string? dir)
    {
        return !string.IsNullOrEmpty(dir) && _cosClient.BucketClient.DirExists(dir);
    }

    public void DeleteDirectory(string dir)
    {
        _cosClient.BucketClient.DeleteDir(dir);
    }
}
