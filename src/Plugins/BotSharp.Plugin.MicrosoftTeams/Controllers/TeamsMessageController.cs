using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.MicrosoftTeams.Controllers;

/// <summary>
/// Inbound Teams activities are handled by MapAgentApplicationEndpoints() registered in
/// MicrosoftTeamsPlugin.Configure — no controller action needed for that path.
/// This controller only exposes the outbound proactive-push API.
/// </summary>
[ApiController]
public class TeamsMessageController : ControllerBase
{
    private readonly ITeamsNotificationService _notification;
    private readonly MicrosoftTeamsSetting _setting;

    public TeamsMessageController(
        ITeamsNotificationService notification,
        MicrosoftTeamsSetting setting)
    {
        _notification = notification;
        _setting = setting;
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
