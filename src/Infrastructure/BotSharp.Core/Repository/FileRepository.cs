using System.IO;
using System.Text.Json;

namespace BotSharp.Core.Repository;

public class FileRepository : IBotSharpRepository
{
    private readonly MyDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private JsonSerializerOptions _options;

    public FileRepository(MyDatabaseSettings dbSettings, IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;

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

            var agentSettings = _services.GetService<AgentSettings>();
            var dir = Path.Combine(_dbSettings.FileRepository, agentSettings.DataDir);
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
            if (_userAgents != null)
            {
                return _userAgents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _userAgents = new List<UserAgentRecord>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var json = File.ReadAllText(Path.Combine(d, "agents.json"));
                _userAgents.AddRange(JsonSerializer.Deserialize<List<UserAgentRecord>>(json, _options));
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

            var convSettings = _services.GetService<ConversationSetting>();
            var dir = Path.Combine(_dbSettings.FileRepository, convSettings.DataDir);
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
                var convSettings = _services.GetService<ConversationSetting>();
                
                foreach (var conversation in _conversations)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository,
                        convSettings.DataDir,
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
                var agentSettings = _services.GetService<AgentSettings>();

                foreach (var agent in _agents)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository,
                        agentSettings.DataDir,
                        agent.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "agent.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(agent, _options));
                }
            }
        }

        return _changedTableNames.Count;
    }
}
