using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<bool> DeleteAgent(string id)
    {
        var user = _db.GetUserById(_user.Id);
        var agent = _db.GetAgentsByUser(_user.Id).FirstOrDefault(x => x.Id.IsEqualTo(id));

        if (user?.Role != UserRole.Admin && agent == null)
        {
            return false;
        }

        var deleted = _db.DeleteAgent(id);
        return await Task.FromResult(deleted);
    }
}
