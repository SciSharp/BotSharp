using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations;
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
}
