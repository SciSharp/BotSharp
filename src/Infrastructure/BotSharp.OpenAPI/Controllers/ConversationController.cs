using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public ConversationController(IServiceProvider services, 
        IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    [HttpPost("/conversation/{agentId}")]
    public async Task<ConversationViewModel> NewConversation([FromRoute] string agentId)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var sess = new Conversation
        {
            UserId = _user.Id,
            AgentId = agentId
        };
        sess = await service.NewConversation(sess);
        return ConversationViewModel.FromSession(sess);
    }

    [HttpDelete("/conversation/{agentId}/{conversationId}")]
    public async Task DeleteConversation([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
    }

    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<MessageResponseModel> SendMessage([FromRoute] string agentId, 
        [FromRoute] string conversationId, 
        [FromBody] NewMessageModel input,
        [FromQuery] string? channel = "openapi")
    {
        var conv = _services.GetRequiredService<IConversationService>();

        var response = new MessageResponseModel();
        var stackMsg = new List<RoleDialogModel>();

        await conv.SendMessage(agentId, conversationId,
            new RoleDialogModel("user", input.Text)
            {
                Channel = channel
            },
            async msg =>
            {
                stackMsg.Add(msg);
            },
            async fnExecuting =>
            {

            },
            async fnExecuted =>
            {
                response.Function = fnExecuted.FunctionName;
                response.Data = fnExecuted.ExecutionData;
            });

        response.Text = string.Join("\r\n", stackMsg.Select(x => x.Content));
        return response;
    }
}
