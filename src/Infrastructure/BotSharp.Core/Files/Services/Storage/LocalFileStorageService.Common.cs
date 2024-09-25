using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public string GetDirectory(string conversationId)
    {
        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, "attachments");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public IEnumerable<string> GetFiles(string relativePath, string? searchPattern = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Enumerable.Empty<string>();
        }

        var path = Path.Combine(_baseDir, relativePath);

        if (!string.IsNullOrWhiteSpace(searchPattern))
        {
            return Directory.GetFiles(path, searchPattern);
        }
        return Directory.GetFiles(path);
    }

    public byte[] GetFileBytes(string fileStorageUrl)
    {
        using var stream = File.OpenRead(fileStorageUrl);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        return bytes;
    }

    public bool SaveFileStreamToPath(string filePath, Stream stream)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }
        return true;
    }

    public bool SaveFileBytesToPath(string filePath, byte[] bytes)
    {
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush();
            fs.Close();
        }
        return true;
    }

    public string GetParentDir(string dir, int level = 1)
    {
        var segs = dir.Split(Path.DirectorySeparatorChar);
        return string.Join(Path.DirectorySeparatorChar, segs.SkipLast(level));
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
