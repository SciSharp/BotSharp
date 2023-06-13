using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Infrastructures.ContentTransmitters;
using BotSharp.Abstraction.Models;
using BotSharp.Core.Conversations.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Core.Conversations;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;

    public ConversationController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("/conversation/session")]
    public async Task<SessionViewModel> NewSession([FromBody] SessionCreationModel session)
    {
        var service = _services.GetRequiredService<ISessionService>();
        var sess = session.ToSession();
        sess = await service.NewSession(sess);
        return SessionViewModel.FromSession(sess);
    }

    [HttpDelete("/conversation/session/{sessionId}")]
    public async Task DeleteSession([FromRoute] string sessionId)
    {
        var service = _services.GetRequiredService<ISessionService>();
    }

    [HttpPost("/conversation/{sessionId}")]
    public async Task<MessageResponseModel> SendMessage([FromBody] NewMessageModel input)
    {
        var transmitter = _services.GetRequiredService<IContentTransfer>();

        var container = new ContentContainer
        {
            Conversations = new List<RoleDialogModel>
            {
                new RoleDialogModel
                {
                    Role = "user",
                    Content = input.Content
                }
            }
        };

        var result = await transmitter.Transport(container);

        return new MessageResponseModel
        {
            Content = container.Output.Content
        };
    }
}
