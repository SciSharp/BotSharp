using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Abstraction.MLTasks;

public interface ILlmProviderService
{
    LlmModelSetting GetSetting(string provider, string model);
    List<string> GetProviders();
    LlmModelSetting GetProviderModel(string provider, string id);
    List<LlmModelSetting> GetProviderModels(string provider);
}
