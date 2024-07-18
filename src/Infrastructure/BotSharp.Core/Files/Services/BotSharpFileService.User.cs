using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class BotSharpFileService
{
    public string GetUserAvatar()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        var dir = GetUserAvatarDir(user?.Id);

        if (!ExistDirectory(dir)) return string.Empty;

        var found = Directory.GetFiles(dir).FirstOrDefault() ?? string.Empty;
        return found;
    }

    public bool SaveUserAvatar(BotSharpFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FileData)) return false;

        try
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var user = db.GetUserById(_user.Id);
            var dir = GetUserAvatarDir(user?.Id);

            if (string.IsNullOrEmpty(dir)) return false;

            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            dir = GetUserAvatarDir(user?.Id, createNewDir: true);
            var (_, bytes) = GetFileInfoFromData(file.FileData);
            File.WriteAllBytes(Path.Combine(dir, file.FileName), bytes);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving user avatar: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }


    #region Private methods
    private string GetUserAvatarDir(string? userId, bool createNewDir = false)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }

        var dir = Path.Combine(_baseDir, USERS_FOLDER, userId, USER_AVATAR_FOLDER);
        if (!Directory.Exists(dir) && createNewDir)
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }
    #endregion
}
