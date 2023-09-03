using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Abstraction.Repositories.Records;
using MongoDB.Driver.Core.Operations;
using System.IO;
using static Tensorflow.TensorShapeProto.Types;
using Tensorflow;
using FunctionDef = BotSharp.Abstraction.Functions.Models.FunctionDef;

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

        if (!agent.Functions.IsEmpty())
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

    public AgentRecord GetAgent(string agentId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            var json = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var record = JsonSerializer.Deserialize<AgentRecord>(json, _options);
            if (record != null && record.Id == agentId)
            {
                var instruction = FetchInstruction(dir);
                var functions = FetchFunctions(dir);
                return record.SetInstruction(instruction).SetFunctions(functions);
            }
        }
        return null;
    }

    private string FetchInstruction(string fileDir)
    {
        var file = Path.Combine(fileDir, "instruction.liquid");
        if (!File.Exists(file)) return null;

        var instruction = File.ReadAllText(file);
        return instruction;
    }

    private List<string> FetchFunctions(string fileDir)
    {
        var file = Path.Combine(fileDir, "functions.json");
        if (!File.Exists(file)) return new List<string>();

        var functionsJson = File.ReadAllText(file);
        var functionDefs = JsonSerializer.Deserialize<List<FunctionDef>>(functionsJson, _options);
        var functions = functionDefs.Select(x => JsonSerializer.Serialize(x, _options)).ToList();
        return functions;
    }

    public void CreateNewConversation(ConversationRecord conversation)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSetting.DataDir, conversation.Id);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var convDir = Path.Combine(dir, "conversation.json");
        if (!File.Exists(convDir))
        {
            File.WriteAllText(convDir, JsonSerializer.Serialize(conversation, _options));
        }

        var dialogDir = Path.Combine(dir, "dialogs.txt");
        if (!File.Exists(dialogDir))
        {
            File.WriteAllText(dialogDir, string.Empty);
        }

        var stateDir = Path.Combine(dir, "state.dict");
        if (!File.Exists(stateDir))
        {
            File.WriteAllText(stateDir, string.Empty);
        }
    }

    public string GetConversationDialog(string conversationId)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var dialogDir = Path.Combine(convDir, "dialogs.txt");
            if (File.Exists(dialogDir))
            {
                return File.ReadAllText(dialogDir);
            }
        }

        return string.Empty;
    }

    public void UpdateConversationDialog(string conversationId, string dialogs)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var dialogDir = Path.Combine(convDir, "dialogs.txt");
            if (File.Exists(dialogDir))
            {
                File.WriteAllText(dialogDir, dialogs);
            }
        }

        return;
    }

    public List<KeyValueModel> GetConversationState(string conversationId)
    {
        var curStates = new List<KeyValueModel>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var stateDir = Path.Combine(convDir, "state.dict");
            if (File.Exists(stateDir))
            {
                var dict = File.ReadAllLines(stateDir);
                foreach (var line in dict)
                {
                    var data = line.Split('=');
                    curStates.Add(new KeyValueModel(data[0], data[1]));
                }
            }
        }

        return curStates;
    }

    private string? FindConversationDirectory(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSetting.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, "conversation.json");
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var conv = JsonSerializer.Deserialize<ConversationRecord>(json, _options);
            if (conv != null && conv.Id == conversationId)
            {
                return d;
            }
        }

        return null;
    }

    public void UpdateConversationState(string conversationId, List<KeyValueModel> state)
    {
        var localStates = new List<string>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var stateDir = Path.Combine(convDir, "state.dict");
            if (File.Exists(stateDir))
            {
                foreach (var data in state)
                {
                    localStates.Add($"{data.Key}={data.Value}");
                }
                File.WriteAllLines(stateDir, localStates);
            }
        }
    }

    public ConversationRecord GetConversation(string conversationId)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var convFile = Path.Combine(convDir, "conversation.json");
            var record = JsonSerializer.Deserialize<ConversationRecord>(convFile);

            var dialogFile = Path.Combine(convDir, "dialogs.txt");
            if (record != null && File.Exists(dialogFile))
            {
                record.Dialog = File.ReadAllText(dialogFile);
            }

            var stateFile = Path.Combine(convDir, "state.dict");
            if (record != null && File.Exists(stateFile))
            {
                var states = File.ReadLines(stateFile);
                record.State = states.Select(x => new KeyValueModel(x.Split('=')[0], x.Split('=')[1])).ToList();
            }

            return record;
        }

        return null;
    }

    public List<ConversationRecord> GetConversations(string userId)
    {
        var records = new List<ConversationRecord>();
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSetting.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, "conversation.json");
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var record = JsonSerializer.Deserialize<ConversationRecord>(json, _options);
            if (record != null && record.UserId == userId)
            {
                var dialogFile = Path.Combine(d, "dialogs.txt");
                if (File.Exists(dialogFile))
                {
                    record.Dialog = File.ReadAllText(dialogFile);
                }

                var stateFile = Path.Combine(d, "state.dict");
                if (File.Exists(stateFile))
                {
                    var states = File.ReadLines(stateFile);
                    record.State = states.Select(x => new KeyValueModel(x.Split('=')[0], x.Split('=')[1])).ToList();
                }

                records.Add(record);
            }
        }

        return records;
    }
}
