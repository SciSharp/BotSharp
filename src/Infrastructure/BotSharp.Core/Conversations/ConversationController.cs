using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Core.Conversations.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Core.Conversations;

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
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();

        var response = new MessageResponseModel();

        await conv.SendMessage(agentId, conversationId, new RoleDialogModel("user", input.Text), async msg =>
        {
            response.Text += msg.Content;
        });

        return response;
    }
}
