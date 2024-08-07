using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileBasicService
{
    public string GetDirectory(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, CONVERSATION_FOLDER, conversationId, "attachments");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public byte[] GetFileBytes(string fileStorageUrl)
    {
        using var stream = File.OpenRead(fileStorageUrl);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        return bytes;
    }

    public bool SavefileToPath(string filePath, Stream stream)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }
        return true;
    }

    public string BuildDirectory(params string[] segments)
    {
        var relativePath = Path.Combine(segments);
        return Path.Combine(_baseDir, relativePath);
    }

    public void CreateDirectory(string dir)
    {
        Directory.CreateDirectory(dir);
    }

    public bool ExistDirectory(string? dir)
    {
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir);
    }

    public void DeleteDirectory(string dir)
    {
        Directory.Delete(dir, true);
    }
}
