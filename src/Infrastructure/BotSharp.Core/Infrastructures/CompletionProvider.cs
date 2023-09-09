using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Infrastructures;

public class CompletionProvider
{
    public static IChatCompletion GetChatCompletion(IServiceProvider services, string modelName = "gpt-3.5-turbo")
    {
        var completions = services.GetServices<IChatCompletion>();
        var settings = services.GetRequiredService<ConversationSetting>();
        // completions.FirstOrDefault(x => x.GetType().FullName.EndsWith(settings.ChatCompletion));
        return completions.FirstOrDefault(x => x.ModelName == modelName);
    }
}
