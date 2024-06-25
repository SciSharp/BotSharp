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
        var dir = Path.Combine(dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public List<Agent> GetAgentsByUser(string userId)
    {
        var agents = _db.GetAgentsByUser(userId);
        return agents;
    }

    public IEnumerable<string> GetAgentTools()
    {
        var tools = typeof(AgentTool).GetFields(BindingFlags.Public | BindingFlags.Static)
                                     .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                                     .Select(x => x.GetRawConstantValue()?.ToString())
                                     .ToList();

        return tools;
    }
}
