using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<bool> DeleteAgent(string id)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var auth = await userService.GetUserAuthorizations(id);

        if (auth.IsAdmin || auth.AgentActions.Contains(UserAction.Edit))
        {
            return false;
        }

        var deleted = _db.DeleteAgent(id);
        return await Task.FromResult(deleted);
    }
}
