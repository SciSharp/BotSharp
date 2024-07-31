namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public string GetUserAvatar()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        var dir = GetUserAvatarDir(user?.Id);

        if (!ExistDirectory(dir)) return string.Empty;

        var found = _cosClient.BucketClient.GetDirFiles(dir).FirstOrDefault() ?? string.Empty;
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

            var (_, bytes) = GetFileInfoFromData(file.FileData);

            var extension = Path.GetExtension(file.FileName);

            var fileName = user?.Id == null ? file.FileName : $"{user?.Id}{extension}";

            return _cosClient.BucketClient.UploadBytes($"{dir}/{fileName}", bytes);
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

        var dir = $"{USERS_FOLDER}/{userId}/{USER_AVATAR_FOLDER}/";

        return dir;
    }
    #endregion
}
