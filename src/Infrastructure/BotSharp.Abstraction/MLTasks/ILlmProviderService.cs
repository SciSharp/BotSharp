using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Abstraction.MLTasks;

public interface ILlmProviderService
{
    LlmModelSetting GetSetting(string provider, string model);
    List<string> GetProviders();
    LlmModelSetting GetProviderModel(string provider, string id, bool? multiModal = null, LlmModelType? modelType = null, bool imageGenerate = false);
    List<LlmModelSetting> GetProviderModels(string provider);
    List<LlmProviderSetting> GetLlmConfigs(LlmConfigOptions? options = null);
}
