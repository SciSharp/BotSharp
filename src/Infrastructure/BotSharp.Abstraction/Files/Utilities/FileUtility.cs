using BotSharp.Abstraction.Repositories.Enums;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;

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

    public static string GetFileContentType(string filePath)
    {
        string contentType;
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out contentType))
        {
            contentType = string.Empty;
        }

        return contentType;
    }
}
