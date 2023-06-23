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
    public async Task<SessionViewModel> NewSession([FromRoute] string agentId)
    {
        var service = _services.GetRequiredService<ISessionService>();
        var sess = new Session
        {
            AgentId = agentId
        };
        sess = await service.NewSession(sess);
        return SessionViewModel.FromSession(sess);
    }

    [HttpDelete("/conversation/{agentId}/{sessionId}")]
    public async Task DeleteSession([FromRoute] string agentId, [FromRoute] string sessionId)
    {
        var service = _services.GetRequiredService<ISessionService>();
    }

    [HttpPost("/conversation/{agentId}/{sessionId}")]
    public async Task<MessageResponseModel> SendMessage([FromRoute] string agentId, 
        [FromRoute] string sessionId, 
        [FromBody] NewMessageModel input)
    {
        var transmitter = _services.GetRequiredService<IContentTransfer>();

        var container = new ContentContainer
        {
            AgentId = agentId,
            SessionId = sessionId,
            Conversations = new List<RoleDialogModel>
            {
                new RoleDialogModel
                {
                    Role = "user",
                    Text = input.Text
                }
            },
            UserId = _user.Id
        };

        var result = await transmitter.Transport(container);

        return new MessageResponseModel
        {
            Content = result.IsSuccess ? container.Output.Text : result.Messages.First()
        };
    }
}
