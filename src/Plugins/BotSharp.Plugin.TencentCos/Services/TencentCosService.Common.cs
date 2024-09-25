namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public string GetDirectory(string conversationId)
    {
        return $"{CONVERSATION_FOLDER}/{conversationId}/attachments/";
    }

    public IEnumerable<string> GetFiles(string relativePath, string? searchPattern = null)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            return _cosClient.BucketClient.GetDirFiles(relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting files: {ex.Message}\r\n{ex.InnerException}");
            return Enumerable.Empty<string>();
        }
    }

    public byte[] GetFileBytes(string fileStorageUrl)
    {
        try
        {
            return _cosClient.BucketClient.DownloadFileBytes(fileStorageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting file bytes: {ex.Message}\r\n{ex.InnerException}");
            return Array.Empty<byte>();
        }
    }

    public bool SaveFileStreamToPath(string filePath, Stream stream)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        try
        {
            return _cosClient.BucketClient.UploadStream(filePath, stream);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving file stream to path: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public bool SaveFileBytesToPath(string filePath, byte[] bytes)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        try
        {
            return _cosClient.BucketClient.UploadBytes(filePath, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving file bytes to path: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public string GetParentDir(string dir, int level = 1)
    {
        var segs = dir.Split("/");
        return string.Join("/", segs.SkipLast(level));
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
