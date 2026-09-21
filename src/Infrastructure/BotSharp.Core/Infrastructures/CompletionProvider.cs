using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Settings;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static object GetCompletion(
        IServiceProvider services, 
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
            return GetAudioTranscriber(services, provider: provider, model: model);
        }
        else if (settings.Type == LlmModelType.Realtime)
        {
            return GetRealTimeCompletion(services, provider: provider, model: model);
        }
        else if (settings.Type == LlmModelType.Live)
        {
            return GetLiveCompletion(services, provider: provider, model: model);
        }
        else
        {
            return GetChatCompletion(services, provider: provider, model: model, agentConfig: agentConfig);
        }
    }

    public static IChatCompletion GetChatCompletion(
        IServiceProvider services, 
        string? provider = null, 
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        bool? realTime = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<IChatCompletion>();
        (provider, model) = GetProviderAndModel(services, provider: provider, model: model, modelId: modelId, 
            multiModal: multiModal,
            agentConfig: agentConfig);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
            // Callers dereference the result immediately, so returning null only turns a wiring
            // problem into a NullReferenceException far from its cause.
            throw new InvalidOperationException($"Can't resolve chat completion provider by '{provider}'. " +
                "Register the plugin that provides it in PluginLoader:Assemblies.");
        }

        completer.SetModelName(model);
        return completer;
    }

    public static ITextCompletion GetTextCompletion(
        IServiceProvider services, 
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

    public static IImageCompletion GetImageCompletion(
        IServiceProvider services,
        string? provider = null,
        string? model = null,
        string? modelId = null,
        IEnumerable<LlmModelCapability>? capabilities = null)
    {
        var completions = services.GetServices<IImageCompletion>();
        (provider, model) = GetProviderAndModel(services, provider: provider, 
            model: model, modelId: modelId, capabilities: capabilities);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
        }

        completer?.SetModelName(model);
        return completer;
    }

    public static ITextEmbedding GetTextEmbedding(
        IServiceProvider services,
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
        completer.SetDimension(found.Embedding?.Dimension ?? 0);
        return completer;
    }

    public static IAudioTranscription GetAudioTranscriber(
        IServiceProvider services,
        string? provider = null,
        string? model = null)
    {
        var settingService = services.GetRequiredService<ISettingService>();
        var completions = services.GetServices<IAudioTranscription>();
        var completer = completions.FirstOrDefault(x => x.Provider == (provider ?? "openai"));
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve audio-transcriber provider by {provider}");
            return default!;
        }

        completer.SetModelName(model ?? Gpt4xModelConstants.GPT_4o_Mini_Transcribe);
        return completer;
    }

    public static IAudioSynthesis GetAudioSynthesizer(
        IServiceProvider services,
        string? provider = null,
        string? model = null)
    {
        var settingService = services.GetRequiredService<ISettingService>();
        var completions = services.GetServices<IAudioSynthesis>();
        var completer = completions.FirstOrDefault(x => x.Provider == (provider ?? "openai"));
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve audio-synthesizer provider by {provider}");
            return default!;
        }

        completer.SetModelName(model ?? Gpt4xModelConstants.GPT_4o_Mini_Tts);
        return completer;
    }

    public static IRealTimeCompletion GetRealTimeCompletion(
        IServiceProvider services,
        string? provider = null,
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<IRealTimeCompletion>();
        (provider, model) = GetProviderAndModel(services, provider: provider, model: model, modelId: modelId,
            multiModal: multiModal,
            modelType: LlmModelType.Realtime,
            agentConfig: agentConfig);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve completion provider by {provider}");
        }

        completer?.SetModelName(model);
        return completer;
    }

    /// <summary>
    /// Live providers are registered as <see cref="ILiveCompletion"/> only, so they are resolved
    /// from their own list: a provider name is never shared between the two voice families here.
    /// </summary>
    public static ILiveCompletion GetLiveCompletion(
        IServiceProvider services,
        string? provider = null,
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        AgentLlmConfig? agentConfig = null)
    {
        var completions = services.GetServices<ILiveCompletion>();
        (provider, model) = GetProviderAndModel(services, provider: provider, model: model, modelId: modelId,
            multiModal: multiModal,
            modelType: LlmModelType.Live,
            agentConfig: agentConfig);

        var completer = completions.FirstOrDefault(x => x.Provider == provider);
        if (completer == null)
        {
            var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
            logger.LogError($"Can't resolve live completion provider by {provider}");
        }

        completer?.SetModelName(model);
        return completer;
    }

    private static (string, string) GetProviderAndModel(
        IServiceProvider services,
        string? provider = null,
        string? model = null,
        string? modelId = null,
        bool? multiModal = null,
        LlmModelType? modelType = null,
        IEnumerable<LlmModelCapability>? capabilities = null,
        AgentLlmConfig? agentConfig = null)
    {
        var agentSetting = services.GetRequiredService<AgentSettings>();
        var state = services.GetRequiredService<IConversationStateService>();

        if (string.IsNullOrEmpty(provider))
        {
            provider = agentConfig?.Provider ?? agentSetting.LlmConfig?.Provider;
            provider = state.GetState(LlmStateConst.PROVIDER, provider ?? "azure-openai");
        }

        if (string.IsNullOrEmpty(model))
        {
            model = agentConfig?.Model ?? agentSetting.LlmConfig?.Model;
            if (state.ContainsState(LlmStateConst.MODEL))
            {
                model = state.GetState(LlmStateConst.MODEL, model ?? "gpt-image-1-mini");
            }
            else if (state.ContainsState(LlmStateConst.MODEL_ID) || !string.IsNullOrEmpty(modelId))
            {
                var modelIdentity = state.ContainsState(LlmStateConst.MODEL_ID) ? state.GetState(LlmStateConst.MODEL_ID) : modelId;
                var llmProviderService = services.GetRequiredService<ILlmProviderService>();
                model = llmProviderService.GetProviderModel(provider, modelIdentity,
                    multiModal: multiModal,
                    modelType: modelType,
                    capabilities: capabilities)?.Name;
            }
        }

        state.SetState(LlmStateConst.PROVIDER, provider);
        state.SetState(LlmStateConst.MODEL, model);
        return (provider, model);
    }
}
