using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static IChatCompletion GetChatCompletion(IServiceProvider services)
    {
        var completions = services.GetServices<IChatCompletion>();
        // var settings = services.GetRequiredService<ConversationSetting>();
        // completions.FirstOrDefault(x => x.GetType().FullName.EndsWith(settings.ChatCompletion));
        var state = services.GetRequiredService<IConversationStateService>();
        var model = state.GetState("model", "gpt-3.5-turbo");
        return completions.FirstOrDefault(x => x.ModelName == model);
    }
}
