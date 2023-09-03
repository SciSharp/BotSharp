using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Records;
using System.IO;

namespace BotSharp.Core.Repository;

public class FileRepository : IBotSharpRepository
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly ConversationSetting _conversationSetting;
    private readonly IServiceProvider _services;
    private JsonSerializerOptions _options;

    public FileRepository(
        IServiceProvider services,
        BotSharpDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        ConversationSetting conversationSetting)
    {
        _services = services;
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _conversationSetting = conversationSetting;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    private List<UserRecord> _users;
    public IQueryable<UserRecord> User
    {
        get
        {
            if (_users != null)
            {
                return _users.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _users = new List<UserRecord>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var json = File.ReadAllText(Path.Combine(d, "user.json"));
                _users.Add(JsonSerializer.Deserialize<UserRecord>(json, _options));
            }
            return _users.AsQueryable();
        }
    }

    private List<AgentRecord> _agents;
    public IQueryable<AgentRecord> Agent
    {
        get
        {
            if (_agents != null)
            {
                return _agents.AsQueryable();
            }

            //var agentSettings = _services.GetService<AgentSettings>();
            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            _agents = new List<AgentRecord>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var json = File.ReadAllText(Path.Combine(d, "agent.json"));
                _agents.Add(JsonSerializer.Deserialize<AgentRecord>(json, _options));
            }
            return _agents.AsQueryable();
        }
    }

    private List<UserAgentRecord> _userAgents;
    public IQueryable<UserAgentRecord> UserAgent
    {
        get
        {
            if (_userAgents != null && _userAgents.Count > 0)
            {
                return _userAgents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _userAgents = new List<UserAgentRecord>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var file = Path.Combine(d, "agents.json");
                if (Directory.Exists(d) && File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    _userAgents.AddRange(JsonSerializer.Deserialize<List<UserAgentRecord>>(json, _options));
                }
            }
            return _userAgents.AsQueryable();
        }
    }

    private List<ConversationRecord> _conversations;
    public IQueryable<ConversationRecord> Conversation
    {
        get
        {
            if (_conversations != null)
            {
                return _conversations.AsQueryable();
            }

            //var convSettings = _services.GetService<ConversationSetting>();
            var dir = Path.Combine(_dbSettings.FileRepository, _conversationSetting.DataDir);
            _conversations = new List<ConversationRecord>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var path = Path.Combine(d, "conversation.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _conversations.Add(JsonSerializer.Deserialize<ConversationRecord>(json, _options));
                }
            }
            return _conversations.AsQueryable();
        }
    }

    private List<RoutingItemRecord> _routingItems;
    public IQueryable<RoutingItemRecord> RoutingItem
    {
        get
        {
            if (_routingItems != null)
            {
                return _routingItems.AsQueryable();
            }

            _routingItems = new List<RoutingItemRecord>();
            //var agentSettings = _services.GetRequiredService<AgentSettings>();
            //var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
            var filePath = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, "route.json");
            if (File.Exists(filePath))
            {
                _routingItems = JsonSerializer.Deserialize<List<RoutingItemRecord>>(File.ReadAllText(filePath));
            }
            
            return _routingItems.AsQueryable();
        }
    }

    private List<RoutingProfileRecord> _routingProfiles;
    public IQueryable<RoutingProfileRecord> RoutingProfile
    {
        get
        {
            if (_routingProfiles != null)
            {
                return _routingProfiles.AsQueryable();
            }

            _routingProfiles = new List<RoutingProfileRecord>();
            //var agentSettings = _services.GetRequiredService<AgentSettings>();
            //var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
            var filePath = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, "routing-profile.json");
            if (File.Exists(filePath))
            {
                _routingProfiles = JsonSerializer.Deserialize<List<RoutingProfileRecord>>(File.ReadAllText(filePath));
            }
            
            return _routingProfiles.AsQueryable();
        }
    }

    public void Add<TTableInterface>(object entity)
    {
        _conversations = Conversation.ToList();
        if (entity is ConversationRecord conversation)
        {
            _conversations.Add(conversation);
            _changedTableNames.Add(nameof(ConversationRecord));
        }
        else if (entity is AgentRecord agent)
        {
            _agents.Add(agent);
            _changedTableNames.Add(nameof(AgentRecord));
        }
        else if (entity is UserRecord user)
        {
            _users.Add(user);
            _changedTableNames.Add(nameof(UserRecord));
        }
        else if (entity is UserAgentRecord userAgent)
        {
            _userAgents.Add(userAgent);
            _changedTableNames.Add(nameof(UserAgentRecord));
        }
    }

    List<string> _changedTableNames = new List<string>();
    public int Transaction<TTableInterface>(Action action)
    {
        _changedTableNames.Clear();
        action();

        // Persist to disk
        foreach (var table in _changedTableNames)
        {
            if (table == nameof(ConversationRecord))
            {
                //var convSettings = _services.GetService<ConversationSetting>();

                foreach (var conversation in _conversations)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository,
                        _conversationSetting.DataDir,
                        conversation.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "conversation.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(conversation, _options));
                }
            }
            else if (table == nameof(AgentRecord))
            {
                //var agentSettings = _services.GetService<AgentSettings>();

                foreach (var agent in _agents)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository,
                        _agentSettings.DataDir,
                        agent.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "agent.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(agent, _options));
                }
            }
            else if (table == nameof(UserRecord))
            {
                foreach (var user in _users)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository,
                        "users",
                        user.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "user.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
                }
            }
            else if (table == nameof(UserAgentRecord))
            {
                _userAgents.GroupBy(x => x.UserId)
                    .Select(x => x.Key).ToList()
                    .ForEach(uid =>
                    {
                        var agents = _userAgents.Where(x => x.UserId == uid).ToList();
                        if (agents.Any())
                        {
                            var dir = Path.Combine(_dbSettings.FileRepository, "users", uid);
                            var path = Path.Combine(dir, "agents.json");
                            File.WriteAllText(path, JsonSerializer.Serialize(agents, _options));
                        }
                    });
            }
        }

        return _changedTableNames.Count;
    }

    public UserRecord GetUserByEmail(string email)
    {
        return User.FirstOrDefault(x => x.Email == email);
    }

    public void CreateUser(UserRecord user)
    {
        var userId = Guid.NewGuid().ToString();
        var dir = Path.Combine(_dbSettings.FileRepository, "users", userId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var path = Path.Combine(dir, "user.json");
        File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
    }

    public void UpdateAgent(AgentRecord agent)
    {
        if (agent == null) return;

        var dir = GetAgentDataDir(agent.Id);

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            var instructionFile = Path.Combine(dir, "instruction.liquid");
            File.WriteAllText(instructionFile, agent.Instruction);
        }

        if (agent.Functions != null || agent.Functions.Any())
        {
            var functionFile = Path.Combine(dir, "functions.json");
            var functions = new List<string>();
            foreach (var function in agent.Functions)
            {
                var functionDef = JsonSerializer.Deserialize<FunctionDef>(function, _options);
                functions.Add(JsonSerializer.Serialize(functionDef, _options));
            }

            var functionText = JsonSerializer.Serialize(functions, _options);
            File.WriteAllText(functionFile, functionText);
        }
    }

    private string GetAgentDataDir(string agentId)
    {
        //var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        //var agentSettings = _services.GetRequiredService<AgentSettings>();
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public void DeleteRoutingItems()
    {
        throw new NotImplementedException();
    }

    public void DeleteRoutingProfiles()
    {
        throw new NotImplementedException();
    }

    public List<RoutingItemRecord> CreateRoutingItems(List<RoutingItemRecord> routingItems)
    {
        throw new NotImplementedException();
    }

    public List<RoutingProfileRecord> CreateRoutingProfiles(List<RoutingProfileRecord> profiles)
    {
        throw new NotImplementedException();
    }

    public List<string> GetAgentResponses(string agentId)
    {
        var responses = new List<string>();
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, "responses");
        if (!Directory.Exists(dir)) return responses;

        foreach (var file in Directory.GetFiles(dir))
        {
            responses.Add(File.ReadAllText(file));
        }

        return responses;
    }
}
