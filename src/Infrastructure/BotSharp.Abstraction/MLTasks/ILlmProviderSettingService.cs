using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Abstraction.MLTasks;

public interface ILlmProviderSettingService
{
    LlmModelSetting GetSetting(string provider, string model);
}
