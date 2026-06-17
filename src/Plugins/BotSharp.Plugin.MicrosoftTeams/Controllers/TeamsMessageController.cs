using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace BotSharp.Plugin.MicrosoftTeams.Controllers;

[ApiController]
public class TeamsMessageController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;
    private readonly TeamsRequestState _requestState;
    private readonly ITeamsNotificationService _notification;
    private readonly MicrosoftTeamsSetting _setting;

    public TeamsMessageController(
        IBotFrameworkHttpAdapter adapter,
        IBot bot,
        TeamsRequestState requestState,
        ITeamsNotificationService notification,
        MicrosoftTeamsSetting setting)
    {
        _adapter = adapter;
        _bot = bot;
        _requestState = requestState;
        _notification = notification;
        _setting = setting;
    }

    /// <summary>
    /// Inbound endpoint registered as the Azure Bot "messaging endpoint".
    /// Authentication is performed by the Bot Framework JWT pipeline inside the adapter,
    /// so the action itself is anonymous.
    /// https://learn.microsoft.com/azure/bot-service/bot-builder-basics
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/teams/messages/{agentId}")]
    public async Task PostAsync([FromRoute] string agentId)
    {
        _requestState.AgentId = agentId;
        await _adapter.ProcessAsync(Request, Response, _bot);
    }

    /// <summary>
    /// Outbound (proactive) push. Requires the platform's standard authorization.
    /// </summary>
    [Authorize]
    [HttpPost("/teams/notify")]
    public async Task<IActionResult> NotifyAsync([FromBody] TeamsNotifyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest("userId is required.");
        }

        bool delivered;
        if (!string.IsNullOrEmpty(request.Prompt))
        {
            var agentId = request.AgentId ?? _setting.AgentId;
            if (string.IsNullOrEmpty(agentId))
            {
                return BadRequest("agentId is required when prompt is set (or configure MicrosoftTeams:AgentId).");
            }
            delivered = await _notification.NotifyAsync(request.UserId, agentId, request.Prompt, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.Text))
        {
            delivered = await _notification.SendTextAsync(request.UserId, request.Text, cancellationToken);
        }
        else
        {
            return BadRequest("Either text or prompt must be provided.");
        }

        return delivered ? Ok(new { success = true }) : NotFound(new { success = false, reason = "No conversation reference for user." });
    }
}
