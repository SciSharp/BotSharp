using System.IO;
using System.Reflection;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService : IAgentService
{
    private readonly IServiceProvider _services;
    private readonly IBotSharpRepository _db;
    private readonly ILogger _logger;
    private readonly IUserIdentity _user;
    private readonly AgentSettings _agentSettings;
    private readonly JsonSerializerOptions _options;

    public AgentService(IServiceProvider services,
        IBotSharpRepository db,
        ILogger<AgentService> logger, 
        IUserIdentity user, 
        AgentSettings agentSettings)
    {
        _services = services;
        _db = db;
        _logger = logger;
        _user = user;
        _agentSettings = agentSettings;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true
        };
    }

    public string GetDataDir()
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        return Path.Combine(dbSettings.FileRepository);
    }

    public string GetAgentDataDir(string agentId)
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public async Task<List<UserAgent>> GetUserAgents(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return [];

        var userAgents = _db.GetUserAgents(userId);
        return userAgents;
    }

    public IEnumerable<AgentUtility> GetAgentUtilityOptions()
    {
        var utilities = new List<AgentUtility>();
        var hooks = _services.GetServices<IAgentUtilityHook>();
        foreach (var hook in hooks)
        {
            hook.AddUtilities(utilities);
        }
        return utilities.Where(x => !string.IsNullOrWhiteSpace(x.Name)).OrderBy(x => x.Name).ToList();
    }
}
