using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static object GetCompletion(IServiceProvider services, 
        string? provider = null, 
        string? model = null, 
        AgentLlmConfig? agentConfig = null)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();

        (provider, model) = GetProviderAndModel(services, provider: provider, model: model, agentConfig: agentConfig);

        var settings = settingsService.GetSetting(provider, model);

        if (settings.Type == LlmModelType.Text)
        {
            return GetTextCompletion(services, 
                provider: provider, 
                model: model, 
                agentConfig: agentConfig);
        }
        else
        {
            return GetChatCompletion(services, 
                provider: provider, 
                model: model, 
                agentConfig: agentConfig);
        }
    }

    public static IChatCompletion GetChatCompletion(IServiceProvider services, 
        string? provider = null, 
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<IChatCompletion>();
        (provider, model) = GetProviderAndModel(services, provider: provider, model: model, modelId: modelId, 
            multiModal: multiModal, agentConfig: agentConfig);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
        }

        completer?.SetModelName(model);

        return completer;
    }

    private static (string, string) GetProviderAndModel(IServiceProvider services, 
        string? provider = null,
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        AgentLlmConfig? agentConfig = null)
    {
        var agentSetting = services.GetRequiredService<AgentSettings>();
        var state = services.GetRequiredService<IConversationStateService>();

        if (string.IsNullOrEmpty(provider))
        {
            provider = agentConfig?.Provider ?? agentSetting.LlmConfig?.Provider;
            provider = state.GetState("provider", provider ?? "azure-openai");
        }

        if (string.IsNullOrEmpty(model))
        {
            model = agentConfig?.Model ?? agentSetting.LlmConfig?.Model;
            if (state.ContainsState("model"))
            {
                model = state.GetState("model", model ?? "gpt-35-turbo-4k");
            }
            else if (state.ContainsState("model_id") || !string.IsNullOrEmpty(modelId))
            {
                var modelIdentity = state.ContainsState("model_id") ? state.GetState("model_id") : modelId;
                var llmProviderService = services.GetRequiredService<ILlmProviderService>();
                model = llmProviderService.GetProviderModel(provider, modelIdentity, multiModal: multiModal)?.Name;
            }
        }

        state.SetState("provider", provider);
        state.SetState("model", model);

        return (provider, model);
    }
    public static ITextCompletion GetTextCompletion(IServiceProvider services, 
        string? provider = null, 
        string? model = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<ITextCompletion>();

        (provider, model) = GetProviderAndModel(services, provider: provider, model: model, agentConfig: agentConfig);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
        }

        completer.SetModelName(model);

        return completer;
    }
}
