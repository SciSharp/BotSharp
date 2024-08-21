using BotSharp.Abstraction.Repositories.Enums;

namespace BotSharp.Abstraction.Files;

public class FileCoreSettings
{
    public string Storage { get; set; } = FileStorageEnum.LocalFileStorage;
    public string Pdf2TextConverter { get; set; }
    public string Pdf2ImageConverter { get; set; }
}
