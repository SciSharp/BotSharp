namespace BotSharp.OpenAPI.Controllers;

public partial class ConversationController
{
    #region Search state keys
    [HttpGet("/conversation/state/keys")]
    public async Task<List<string>> GetConversationStateKeys([FromQuery] ConversationStateKeysFilter request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var keys = await convService.GetConversationStateSearhKeys(request);
        return keys;
    }
    #endregion

    #region Migrate Latest States
    [HttpPost("/conversation/latest-state/migrate")]
    public async Task<bool> MigrateConversationLatestStates([FromBody] MigrateLatestStateRequest request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var res = await convService.MigrateLatestStates(request.BatchSize, request.ErrorLimit);
        return res;
    }
    #endregion
}
