using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.MicrosoftTeams.Controllers;

[ApiController]
public class TeamsMessageController : ControllerBase
{
    private readonly IAgentHttpAdapter _adapter;
    private readonly IAgent _bot;
    private readonly ITeamsNotificationService _notification;
    private readonly MicrosoftTeamsSetting _setting;

    public TeamsMessageController(
        IAgentHttpAdapter adapter,
        IAgent bot,
        ITeamsNotificationService notification,
        MicrosoftTeamsSetting setting)
    {
        _adapter = adapter;
        _bot = bot;
        _notification = notification;
        _setting = setting;
    }

    /// <summary>
    /// Inbound endpoint for Teams activity payloads from Azure Bot Service.
    /// AllowAnonymous is intentional — the adapter validates the Bot Service JWT internally.
    /// Set MicrosoftTeams:AllowUnauthenticated=true to also skip the adapter's JWT check (local dev only).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/teams/messages/{agentId}")]
    public async Task PostAsync([FromRoute] string agentId, CancellationToken cancellationToken)
    {
        Request.Headers.Remove("Authorization");
        await _adapter.ProcessAsync(Request, Response, _bot, cancellationToken);
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
