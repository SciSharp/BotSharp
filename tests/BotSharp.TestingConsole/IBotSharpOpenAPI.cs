using BotSharp.Abstraction.Conversations.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;
using Refit;

namespace BotSharp.TestingConsole;

public interface IBotSharpOpenAPI
{
    [Post("/conversation/{agentId}")]
    Task<ConversationViewModel> NewConversation([Header("Authorization")] string authorization, string agentId);

    [Post("/conversation/{agentId}/{conversationId}")]
    Task<MessageResponseModel> SendMessage([Header("Authorization")] string authorization, string agentId, string conversationId, [Body] NewMessageModel input);

    [Post("/instruct/text-completion")]
    Task<string> TextCompletion([Header("Authorization")] string authorization, [Body] IncomingMessageModel message);
}
