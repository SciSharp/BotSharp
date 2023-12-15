using BotSharp.Abstraction.Agents.Models;
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
        var state = services.GetRequiredService<IConversationStateService>();
        var agentSetting = services.GetRequiredService<AgentSettings>();

        if (string.IsNullOrEmpty(provider))
        {
            provider = agentConfig?.Provider ?? agentSetting.LlmConfig?.Provider;
            provider = state.GetState("provider", provider ?? "azure-openai");
        }

        if (string.IsNullOrEmpty(model))
        {
            model = agentConfig?.Model ?? agentSetting.LlmConfig?.Model;
            model = state.GetState("model", model ?? "gpt-35-turbo-4k");
        }

        var settingsService = services.GetRequiredService<ILlmProviderSettingService>();
        var settings = settingsService.GetSetting(provider, model);

        if (settings.Type == LlmModelType.Text)
        {
            return GetTextCompletion(services, provider: provider, model: model);
        }
        else
        {
            return GetChatCompletion(services, provider: provider, model: model);
        }
    }

    public static IChatCompletion GetChatCompletion(IServiceProvider services, 
        string? provider = null, 
        string? model = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<IChatCompletion>();
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
            model = state.GetState("model", model ?? "gpt-35-turbo-4k");
        }

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
        }

        completer.SetModelName(model);

        return completer;
    }

    public static ITextCompletion GetTextCompletion(IServiceProvider services, 
        string? provider = null, 
        string? model = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<ITextCompletion>();
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
            model = state.GetState("model", model ?? "gpt-35-turbo-instruct");
        }

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
