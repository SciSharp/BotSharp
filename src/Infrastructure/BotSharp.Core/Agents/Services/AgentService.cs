using Microsoft.Extensions.Logging;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService : IAgentService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IUserIdentity _user;
    private readonly AgentSettings _settings;

    public AgentService(IServiceProvider services, ILogger<AgentService> logger, IUserIdentity user, AgentSettings settings)
    {
        _services = services;
        _logger = logger;
        _user = user;
        _settings = settings;
    }

    public string GetAgentDataDir(string agentId)
    {
        var dir = Path.Combine(_settings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }
}
