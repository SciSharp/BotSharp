using BotSharp.Abstraction.Repositories.Enums;

namespace BotSharp.Abstraction.Files;

public class FileStorageSettings
{
    public string Default { get; set; } = FileStorageEnum.LocalFileStorage;
}
