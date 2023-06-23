using Azure.AI.OpenAI;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Infrastructures.ContentTransmitters;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Services;

public class ChatCompletionService : IChatServiceZone
{
    private readonly IChatCompletion _chatCompletion;

    public ChatCompletionService(IChatCompletion chatCompletion)
    {
        _chatCompletion = chatCompletion;
    }

    public int Priority => 100;

    public async Task Serving(ContentContainer content)
    {
        var output = await _chatCompletion.GetChatCompletionsAsync(content.Conversations);

        content.Output = new RoleDialogModel
        {
            Role = ChatRole.Assistant.ToString(),
            Text = output
        };
    }
}
