using BotSharp.Abstraction.Repositories.Enums;

namespace BotSharp.Abstraction.Files;

public class FileCoreSettings
{
    public string Storage { get; set; } = FileStorageEnum.LocalFileStorage;
    public SettingBase Pdf2TextConverter { get; set; }
    public SettingBase Pdf2ImageConverter { get; set; }
}

public class SettingBase
{
    public string Provider { get; set; }
}