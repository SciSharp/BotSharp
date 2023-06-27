using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService : IAgentService
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public AgentService(IServiceProvider services, IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    public string GetAgentDataDir(string agentId)
    {
        var dir = Path.Combine("data", agentId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }
}
