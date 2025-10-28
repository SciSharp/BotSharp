using BotSharp.Abstraction.Agents.Options;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<bool> DeleteAgent(string id, AgentDeleteOptions? options = null)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var auth = await userService.GetUserAuthorizations(new List<string> { id });

        if (!auth.IsAgentActionAllowed(id, UserAction.Edit))
        {
            return false;
        }

        var deleted = _db.DeleteAgent(id, options);
        return await Task.FromResult(deleted);
    }
}
