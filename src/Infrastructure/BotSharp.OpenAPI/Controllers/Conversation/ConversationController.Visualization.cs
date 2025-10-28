using BotSharp.Abstraction.Chart;

namespace BotSharp.OpenAPI.Controllers;

public partial class ConversationController
{
    #region Chart
    [AllowAnonymous]
    [HttpGet("/conversation/{conversationId}/message/{messageId}/user/chart/data")]
    public async Task<ConversationChartDataResponse?> GetConversationChartData(
        [FromRoute] string conversationId,
        [FromRoute] string messageId,
        [FromQuery] ConversationChartDataRequest request)
    {
        var chart = _services.GetServices<IChartProcessor>().FirstOrDefault(x => x.Provider == request?.ChartProvider);
        if (chart == null) return null;

        var result = await chart.GetConversationChartDataAsync(conversationId, messageId, request);
        return ConversationChartDataResponse.From(result);
    }
    #endregion

    #region Dashboard
    [HttpPut("/agent/{agentId}/conversation/{conversationId}/dashboard")]
    public async Task<bool> PinConversationToDashboard([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var pinned = await userService.AddDashboardConversation(conversationId);
        return pinned;
    }

    [HttpDelete("/agent/{agentId}/conversation/{conversationId}/dashboard")]
    public async Task<bool> UnpinConversationFromDashboard([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var unpinned = await userService.RemoveDashboardConversation(conversationId);
        return unpinned;
    }
    #endregion
}
