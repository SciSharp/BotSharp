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
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<IChatCompletion>();
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

    private static (string, string) GetProviderAndModel(IServiceProvider services, 
        string? provider = null,
        string? model = null,
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
            else if (state.ContainsState("model_id"))
            {
                var modelId = state.GetState("model_id");
                var llmProviderService = services.GetRequiredService<ILlmProviderService>();
                model = llmProviderService.GetProviderModel(provider, modelId)?.Name;
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
