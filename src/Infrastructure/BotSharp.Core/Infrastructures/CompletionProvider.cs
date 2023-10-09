using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
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
            model = state.GetState("model", "gpt-3.5-turbo");
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
            model = state.GetState("model", "gpt-3.5-turbo");
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
