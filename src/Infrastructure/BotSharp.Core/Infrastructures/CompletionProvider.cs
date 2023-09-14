using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static IChatCompletion GetChatCompletion(IServiceProvider services, string? model = null)
    {
        var completions = services.GetServices<IChatCompletion>();

        var state = services.GetRequiredService<IConversationStateService>();
        if (model == null)
        {
            model = state.GetState("model", "gpt-3.5-turbo");
        }
        
        return completions.FirstOrDefault(x => x.ModelName == model);
    }
}
