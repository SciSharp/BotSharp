using BotSharp.Abstraction.Repositories.Enums;

namespace BotSharp.Abstraction.Files;

public class FileCoreSettings
{
    public string Storage { get; set; } = FileStorageEnum.LocalFileStorage;
    public SettingBase? ImageConverter { get; set; }
}