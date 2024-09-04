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
            return GetTextCompletion(services, provider: provider, model: model, agentConfig: agentConfig);
        }
        else if (settings.Type == LlmModelType.Embedding)
        {
            return GetTextEmbedding(services, provider: provider, model: model);
        }
        else if (settings.Type == LlmModelType.Image)
        {
            return GetImageCompletion(services, provider: provider, model: model);
        }
        else if (settings.Type == LlmModelType.Audio)
        {
            return GetAudioCompletion(services, provider: provider, model: model);
        }
        else
        {
            return GetChatCompletion(services, provider: provider, model: model, agentConfig: agentConfig);
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

    public static IImageCompletion GetImageCompletion(IServiceProvider services,
        string? provider = null,
        string? model = null,
        string? modelId = null,
        bool imageGenerate = false)
    {
        var completions = services.GetServices<IImageCompletion>();
        (provider, model) = GetProviderAndModel(services, provider: provider, 
            model: model, modelId: modelId, imageGenerate: imageGenerate);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
        }

        completer?.SetModelName(model);
        return completer;
    }

    public static ITextEmbedding GetTextEmbedding(IServiceProvider services,
        string? provider = null,
        string? model = null)
    {
        var completions = services.GetServices<ITextEmbedding>();
        (provider, model) = GetProviderAndModel(services, provider: provider, model: model);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve text-embedding provider by {provider}");
        }


        var llmProviderService = services.GetRequiredService<ILlmProviderService>();
        var found = llmProviderService.GetSetting(provider, model);

        completer.SetModelName(model);
        completer.SetDimension(found.Dimension);
        return completer;
    }

    public static IAudioCompletion GetAudioCompletion(
        IServiceProvider services,
        string provider,
        string model)
    {
        var completions = services.GetServices<IAudioCompletion>();
        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve audio-completion provider by {provider}");
        }

        completer.SetModelName(model);
        return completer;
    }

    private static (string, string) GetProviderAndModel(IServiceProvider services,
        string? provider = null,
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        bool imageGenerate = false,
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
                model = state.GetState("model", model ?? "dall-e-3");
            }
            else if (state.ContainsState("model_id") || !string.IsNullOrEmpty(modelId))
            {
                var modelIdentity = state.ContainsState("model_id") ? state.GetState("model_id") : modelId;
                var llmProviderService = services.GetRequiredService<ILlmProviderService>();
                model = llmProviderService.GetProviderModel(provider, modelIdentity,
                    multiModal: multiModal, imageGenerate: imageGenerate)?.Name;
            }
        }

        state.SetState("provider", provider);
        state.SetState("model", model);
        return (provider, model);
    }
}
