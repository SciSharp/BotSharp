using Microsoft.AspNetCore.StaticFiles;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public string GetDirectory(string conversationId)
    {
        return $"{CONVERSATION_FOLDER}/{conversationId}/attachments/";
    }

    public (string, byte[]) GetFileInfoFromData(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return (string.Empty, new byte[0]);
        }

        var typeStartIdx = data.IndexOf(':');
        var typeEndIdx = data.IndexOf(';');
        var contentType = data.Substring(typeStartIdx + 1, typeEndIdx - typeStartIdx - 1);

        var base64startIdx = data.IndexOf(',');
        var base64Str = data.Substring(base64startIdx + 1);

        return (contentType, Convert.FromBase64String(base64Str));
    }

    public string GetFileContentType(string filePath)
    {
        string contentType;
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out contentType))
        {
            contentType = string.Empty;
        }

        return contentType;
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
}
