using BotSharp.Abstraction.Repositories;
using System.IO;
using FunctionDef = BotSharp.Abstraction.Functions.Models.FunctionDef;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Agents.Enums;

namespace BotSharp.Core.Repository;

public class FileRepository : IBotSharpRepository
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly ConversationSetting _conversationSettings;
    private JsonSerializerOptions _options;

    public FileRepository(
        BotSharpDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        ConversationSetting conversationSettings)
    {
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _conversationSettings = conversationSettings;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    private List<User> _users;
    public IQueryable<User> Users
    {
        get
        {
            if (!_users.IsNullOrEmpty())
            {
                return _users.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _users = new List<User>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var json = File.ReadAllText(Path.Combine(d, "user.json"));
                _users.Add(JsonSerializer.Deserialize<User>(json, _options));
            }
            return _users.AsQueryable();
        }
    }

    private List<Agent> _agents;
    public IQueryable<Agent> Agents
    {
        get
        {
            if (!_agents.IsNullOrEmpty())
            {
                return _agents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            _agents = new List<Agent>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var json = File.ReadAllText(Path.Combine(d, "agent.json"));
                var agent = JsonSerializer.Deserialize<Agent>(json, _options);
                if (agent != null)
                {
                    agent = agent.SetInstruction(FetchInstruction(d))
                                 .SetTemplates(FetchTemplates(d))
                                 .SetFunctions(FetchFunctions(d))
                                 .SetResponses(FetchResponses(d));
                    _agents.Add(agent);
                }
            }
            return _agents.AsQueryable();
        }
    }

    private List<UserAgent> _userAgents;
    public IQueryable<UserAgent> UserAgents
    {
        get
        {
            if (!_userAgents.IsNullOrEmpty())
            {
                return _userAgents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _userAgents = new List<UserAgent>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var file = Path.Combine(d, "agents.json");
                if (Directory.Exists(d) && File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    _userAgents.AddRange(JsonSerializer.Deserialize<List<UserAgent>>(json, _options));
                }
            }
            return _userAgents.AsQueryable();
        }
    }

    private List<Conversation> _conversations;
    public IQueryable<Conversation> Conversations
    {
        get
        {
            if (!_conversations.IsNullOrEmpty())
            {
                return _conversations.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);
            _conversations = new List<Conversation>();
            foreach (var d in Directory.GetDirectories(dir))
            {
                var path = Path.Combine(d, "conversation.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _conversations.Add(JsonSerializer.Deserialize<Conversation>(json, _options));
                }
            }
            return _conversations.AsQueryable();
        }
    }

    private List<RoutingItem> _routingItems;
    public IQueryable<RoutingItem> RoutingItems
    {
        get
        {
            if (!_routingItems.IsNullOrEmpty())
            {
                return _routingItems.AsQueryable();
            }

            _routingItems = new List<RoutingItem>();
            var filePath = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, "route.json");
            if (File.Exists(filePath))
            {
                _routingItems = JsonSerializer.Deserialize<List<RoutingItem>>(File.ReadAllText(filePath), _options);
            }
            
            return _routingItems.AsQueryable();
        }
    }

    private List<RoutingProfile> _routingProfiles;
    public IQueryable<RoutingProfile> RoutingProfiles
    {
        get
        {
            if (!_routingProfiles.IsNullOrEmpty())
            {
                return _routingProfiles.AsQueryable();
            }

            _routingProfiles = new List<RoutingProfile>();
            var filePath = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, "routing-profile.json");
            if (File.Exists(filePath))
            {
                _routingProfiles = JsonSerializer.Deserialize<List<RoutingProfile>>(File.ReadAllText(filePath), _options);
            }
            
            return _routingProfiles.AsQueryable();
        }
    }

    public void Add<TTableInterface>(object entity)
    {
        if (entity is Conversation conversation)
        {
            _conversations.Add(conversation);
            _changedTableNames.Add(nameof(Conversation));
        }
        else if (entity is Agent agent)
        {
            _agents.Add(agent);
            _changedTableNames.Add(nameof(Agent));
        }
        else if (entity is User user)
        {
            _users.Add(user);
            _changedTableNames.Add(nameof(User));
        }
        else if (entity is UserAgent userAgent)
        {
            _userAgents.Add(userAgent);
            _changedTableNames.Add(nameof(UserAgent));
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
            if (table == nameof(Conversation))
            {
                foreach (var conversation in _conversations)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository,
                        _conversationSettings.DataDir,
                        conversation.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "conversation.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(conversation, _options));
                }
            }
            else if (table == nameof(Agent))
            {
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
            else if (table == nameof(User))
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
            else if (table == nameof(UserAgent))
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

    
    #region Agent
    public void UpdateAgent(Agent agent, AgentField field)
    {
        if (agent == null) return;

        var dir = GetAgentDataDir(agent.Id);

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            var instructionFile = Path.Combine(dir, "instruction.liquid");
            File.WriteAllText(instructionFile, agent.Instruction);
        }

        if (!agent.Functions.IsNullOrEmpty())
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

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
    {
        var responses = new List<string>();
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, "responses");
        if (!Directory.Exists(dir)) return responses;

        foreach (var file in Directory.GetFiles(dir))
        {
            if (file.Split(Path.DirectorySeparatorChar)
                .Last()
                .StartsWith(prefix + "." + intent))
            {
                responses.Add(File.ReadAllText(file));
            }
        }

        return responses;
    }

    public Agent GetAgent(string agentId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            var json = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var record = JsonSerializer.Deserialize<Agent>(json, _options);
            if (record != null && record.Id == agentId)
            {
                var instruction = FetchInstruction(dir);
                var functions = FetchFunctions(dir);
                return record.SetInstruction(instruction).SetFunctions(functions);
            }
        }
        return null;
    }

    public string GetAgentTemplate(string agentId, string templateName)
    {
        var fileDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(fileDir)) return string.Empty;

        var lowerTemplateName = templateName?.ToLower();
        foreach (var file in Directory.GetFiles(fileDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var name = splits[0];
            var extension = splits[1];
            if (name == lowerTemplateName && extension == "liquid")
            {
                return File.ReadAllText(file);
            }
        }

        return string.Empty;
    }
    #endregion

    #region Conversation
    public void CreateNewConversation(Conversation conversation)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, conversation.Id);
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

    public List<StateKeyValue> GetConversationStates(string conversationId)
    {
        var curStates = new List<StateKeyValue>();
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
                    curStates.Add(new StateKeyValue(data[0], data[1]));
                }
            }
        }

        return curStates;
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        var localStates = new List<string>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var stateDir = Path.Combine(convDir, "state.dict");
            if (File.Exists(stateDir))
            {
                foreach (var data in states)
                {
                    localStates.Add($"{data.Key}={data.Value}");
                }
                File.WriteAllLines(stateDir, localStates);
            }
        }
    }

    public Conversation GetConversation(string conversationId)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var convFile = Path.Combine(convDir, "conversation.json");
            var content = File.ReadAllText(convFile);
            var record = JsonSerializer.Deserialize<Conversation>(content, _options);

            var dialogFile = Path.Combine(convDir, "dialogs.txt");
            if (record != null && File.Exists(dialogFile))
            {
                record.Dialog = File.ReadAllText(dialogFile);
            }

            var stateFile = Path.Combine(convDir, "state.dict");
            if (record != null && File.Exists(stateFile))
            {
                var states = File.ReadLines(stateFile);
                record.States = new ConversationState(states.Select(x => new StateKeyValue(x.Split('=')[0], x.Split('=')[1])).ToList());
            }

            return record;
        }

        return null;
    }

    public List<Conversation> GetConversations(string userId)
    {
        var records = new List<Conversation>();
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, "conversation.json");
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var record = JsonSerializer.Deserialize<Conversation>(json, _options);
            if (record != null && record.UserId == userId)
            {
                records.Add(record);
            }
        }

        return records;
    }
    #endregion

    #region User
    public User GetUserByEmail(string email)
    {
        return Users.FirstOrDefault(x => x.Email == email);
    }

    public void CreateUser(User user)
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
    #endregion

    #region Routing
    public void DeleteRoutingItems()
    {
        throw new NotImplementedException();
    }

    public void DeleteRoutingProfiles()
    {
        throw new NotImplementedException();
    }

    public List<RoutingItem> CreateRoutingItems(List<RoutingItem> routingItems)
    {
        throw new NotImplementedException();
    }

    public List<RoutingProfile> CreateRoutingProfiles(List<RoutingProfile> profiles)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Private methods
    private string GetAgentDataDir(string agentId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    private string FetchInstruction(string fileDir)
    {
        var file = Path.Combine(fileDir, "instruction.liquid");
        if (!File.Exists(file)) return string.Empty;

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

    private List<AgentTemplate> FetchTemplates(string fileDir)
    {
        var templates = new List<AgentTemplate>();

        foreach (var file in Directory.GetFiles(fileDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var name = splits[0];
            var extension = splits[1];
            if (name != "instruction" && extension == "liquid")
            {
                var content = File.ReadAllText(file);
                templates.Add(new AgentTemplate(name, content));
            }
        }

        return templates;
    }

    private List<AgentResponse> FetchResponses(string fileDir)
    {
        var responses = new List<AgentResponse>();
        var responseDir = Path.Combine(fileDir, "responses");
        if (!Directory.Exists(responseDir)) return responses;

        foreach (var file in Directory.GetFiles(responseDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var prefix = splits[0];
            var intent = splits[1];
            var content = File.ReadAllText(file);
            responses.Add(new AgentResponse(prefix, intent, content));
        }

        return responses;
    }

    private string? FindConversationDirectory(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, "conversation.json");
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var conv = JsonSerializer.Deserialize<Conversation>(json, _options);
            if (conv != null && conv.Id == conversationId)
            {
                return d;
            }
        }

        return null;
    }
    #endregion
}
