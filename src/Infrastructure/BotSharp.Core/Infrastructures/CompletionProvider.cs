using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static IChatCompletion GetChatCompletion(IServiceProvider services, string? provider = null)
    {
        var completions = services.GetServices<IChatCompletion>();

        var state = services.GetRequiredService<IConversationStateService>();
        if (provider == null)
        {
            provider = state.GetState("provider", "azure-gpt-3.5");
        }
        
        return completions.FirstOrDefault(x => x.Provider == provider);
    }
}
