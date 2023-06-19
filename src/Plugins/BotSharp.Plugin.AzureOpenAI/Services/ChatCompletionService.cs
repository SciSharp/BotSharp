using Azure.AI.OpenAI;
using BotSharp.Abstraction.Infrastructures.ContentTransfers;
using BotSharp.Abstraction.Infrastructures.ContentTransmitters;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Services;

public class ChatCompletionService : IServiceZone
{
    private readonly IChatCompletion _chatCompletion;

    public ChatCompletionService(IChatCompletion chatCompletion)
    {
        _chatCompletion = chatCompletion;
    }

    public async Task Serving(ContentContainer content)
    {
        var output = await _chatCompletion.GetChatCompletionsAsync(content.Conversations);

        content.Output = new RoleDialogModel
        {
            Role = ChatRole.Assistant.ToString(),
            Content = output
        };
    }
}
