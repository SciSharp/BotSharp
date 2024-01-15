using System.Text.Json;

namespace BotSharp.Abstraction.Settings;

/// <summary>
/// This settings service is used to cache, monitor changes and encrypt settings in the system and user level
/// </summary>
public interface ISettingService
{
    T Bind<T>(string path) where T : new();

    object GetDetail(string settingName, bool mask = false);
}
