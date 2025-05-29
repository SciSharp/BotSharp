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
            _logger.LogWarning(ex, $"Error when getting files (path: {relativePath}).");
            return Enumerable.Empty<string>();
        }
    }

    public BinaryData GetFileBytes(string fileStorageUrl)
    {
        try
        {
            var bytes = _cosClient.BucketClient.DownloadFileBytes(fileStorageUrl);
            return BinaryData.FromBytes(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when getting file bytes (url: {fileStorageUrl}).");
            return BinaryData.Empty;
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
            _logger.LogWarning(ex, $"Error when saving file stream to path ({filePath}).");
            return false;
        }
    }

    public bool SaveFileBytesToPath(string filePath, BinaryData binary)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        try
        {
            return _cosClient.BucketClient.UploadBytes(filePath, binary.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when saving file bytes to path ({filePath}).");
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
