using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<bool> DeleteAgent(string id)
    {
        var user = _db.GetUserById(_user.Id);
        var userAgents = await GetUserAgents(user?.Id);
        var found = userAgents?.FirstOrDefault(x => x.AgentId == id);

        if (!UserConstant.AdminRoles.Contains(user?.Role) && (found?.Actions == null || !found.Actions.Contains(UserAction.Edit)))
        {
            return false;
        }

        var deleted = _db.DeleteAgent(id);
        return await Task.FromResult(deleted);
    }
}
