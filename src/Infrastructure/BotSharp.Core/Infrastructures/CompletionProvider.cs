using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static object GetCompletion(IServiceProvider services, string? provider = null, string? model = null)
    {
        var state = services.GetRequiredService<IConversationStateService>();

        if (string.IsNullOrEmpty(provider))
        {
            provider = state.GetState("provider", "azure-openai");
        }

        if (string.IsNullOrEmpty(model))
        {
            model = state.GetState("model", "gpt-35-turbo-instruct");
        }

        var settingsService = services.GetRequiredService<ILlmProviderSettingService>();
        var settings = settingsService.GetSetting(provider, model);

        if(settings.Type == LlmModelType.Text)
        {
            var completions = services.GetServices<ITextCompletion>();
            var completer = completions.FirstOrDefault(x => x.Provider == provider);
            if (completer == null)
            {
                var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
                logger.LogError($"Can't resolve text completion provider by {provider}");
            }

            completer.SetModelName(model);

            return completer;
        }
        else
        {
            var completions = services.GetServices<IChatCompletion>();
            var completer = completions.FirstOrDefault(x => x.Provider == provider);
            if (completer == null)
            {
                var logger = services.GetRequiredService<ILogger<CompletionProvider>>();
                logger.LogError($"Can't resolve chat completion provider by {provider}");
            }

            completer.SetModelName(model);

            return completer;
        }
    }

    public static IChatCompletion GetChatCompletion(IServiceProvider services, string? provider = null, string? model = null)
    {
        var completions = services.GetServices<IChatCompletion>();

        var state = services.GetRequiredService<IConversationStateService>();

        if (string.IsNullOrEmpty(provider))
        {
            provider = state.GetState("provider", "azure-openai");
        }

        if (string.IsNullOrEmpty(model))
        {
            model = state.GetState("model", "gpt-35-turbo-4k");
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

    public static ITextCompletion GetTextCompletion(IServiceProvider services, string? provider = null, string? model = null)
    {
        var completions = services.GetServices<ITextCompletion>();

        var state = services.GetRequiredService<IConversationStateService>();

        if (string.IsNullOrEmpty(provider))
        {
            provider = state.GetState("provider", "azure-openai");
        }

        if (string.IsNullOrEmpty(model))
        {
            model = state.GetState("model", "gpt-35-turbo-instruct");
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
