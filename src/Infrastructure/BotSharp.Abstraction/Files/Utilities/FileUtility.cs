using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

namespace BotSharp.Abstraction.Files.Utilities;

public static class FileUtility
{
    /// <summary>
    /// Get file bytes and content type from data, e.g., "data:image/png;base64,aaaaaaaaa"
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static (string, byte[]) GetFileInfoFromData(string data)
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

    public static string BuildFileDataFromFile(string fileName, byte[] bytes)
    {
        var contentType = GetFileContentType(fileName);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{contentType};base64,{base64}";
    }

    public static string BuildFileDataFromFile(IFormFile file)
    {
        using var stream = new MemoryStream();
        file.CopyTo(stream);
        stream.Position = 0;
        var contentType = GetFileContentType(file.FileName);
        var base64 = Convert.ToBase64String(stream.ToArray());
        stream.Close();

        return $"data:{contentType};base64,{base64}";
    }

    public static string GetFileContentType(string fileName)
    {
        string contentType;
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out contentType))
        {
            contentType = string.Empty;
        }

        return contentType;
    }
}
