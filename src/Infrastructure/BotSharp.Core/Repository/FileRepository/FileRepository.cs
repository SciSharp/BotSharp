using BotSharp.Abstraction.Repositories;
using System.IO;
using FunctionDef = BotSharp.Abstraction.Functions.Models.FunctionDef;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Agents.Models;
using MongoDB.Driver;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Evaluations.Settings;
using System.Text.Encodings.Web;
using BotSharp.Abstraction.Plugins.Models;

namespace BotSharp.Core.Repository;

public partial class FileRepository : IBotSharpRepository
{
    private readonly IServiceProvider _services;
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly ConversationSetting _conversationSettings;
    private JsonSerializerOptions _options;

    private const string AGENT_FILE = "agent.json";
    private const string AGENT_INSTRUCTION_FILE = "instruction";
    private const string AGENT_FUNCTIONS_FILE = "functions.json";
    private const string AGENT_SAMPLES_FILE = "samples.txt";
    private const string USER_FILE = "user.json";
    private const string USER_AGENT_FILE = "agents.json";
    private const string CONVERSATION_FILE = "conversation.json";
    private const string DIALOG_FILE = "dialogs.txt";
    private const string STATE_FILE = "state.json";
    private const string EXECUTION_LOG_FILE = "execution.log";
    private const string PLUGIN_CONFIG_FILE = "config.json";

    public FileRepository(
        IServiceProvider services,
        BotSharpDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        ConversationSetting conversationSettings)
    {
        _services = services;
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _conversationSettings = conversationSettings;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        _dbSettings.FileRepository = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dbSettings.FileRepository);
    }

    private List<User> _users = new List<User>();
    private List<Agent> _agents = new List<Agent>();
    private List<UserAgent> _userAgents = new List<UserAgent>();
    private List<Conversation> _conversations = new List<Conversation>();
    private PluginConfig? _pluginConfig = null;

    private IQueryable<User> Users
    {
        get
        {
            if (!_users.IsNullOrEmpty())
            {
                return _users.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _users = new List<User>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var userFile = Path.Combine(d, USER_FILE);
                    if (!Directory.Exists(d) || !File.Exists(userFile))
                        continue;

                    var json = File.ReadAllText(userFile);
                    _users.Add(JsonSerializer.Deserialize<User>(json, _options));
                }
            }
            return _users.AsQueryable();
        }
    }

    private IQueryable<Agent> Agents
    {
        get
        {
            if (!_agents.IsNullOrEmpty())
            {
                return _agents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            _agents = new List<Agent>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var file = Path.Combine(d, AGENT_FILE);
                    if (!Directory.Exists(d) || !File.Exists(file))
                        continue;

                    var json = File.ReadAllText(file);
                    var agent = JsonSerializer.Deserialize<Agent>(json, _options);
                    if (agent != null)
                    {
                        agent = agent.SetInstruction(FetchInstruction(d))
                                     .SetTemplates(FetchTemplates(d))
                                     .SetFunctions(FetchFunctions(d))
                                     .SetResponses(FetchResponses(d))
                                     .SetSamples(FetchSamples(d));
                        _agents.Add(agent);
                    }
                }
            }
            return _agents.AsQueryable();
        }
    }

    private IQueryable<UserAgent> UserAgents
    {
        get
        {
            if (!_userAgents.IsNullOrEmpty())
            {
                return _userAgents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _userAgents = new List<UserAgent>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var file = Path.Combine(d, USER_AGENT_FILE);
                    if (!Directory.Exists(d) || !File.Exists(file))
                        continue;

                    var json = File.ReadAllText(file);
                    _userAgents.AddRange(JsonSerializer.Deserialize<List<UserAgent>>(json, _options));
                }
            }
            return _userAgents.AsQueryable();
        }
    }


    #region Private methods
    private string GetAgentDataDir(string agentId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            dir = string.Empty;
        }
        return dir;
    }

    private (Agent?, string) GetAgentFromFile(string agentId)
    {
        var dir = GetAgentDataDir(agentId);
        var agentFile = Path.Combine(dir, AGENT_FILE);
        if (!File.Exists(agentFile)) return (null, string.Empty);

        var json = File.ReadAllText(agentFile);
        var agent = JsonSerializer.Deserialize<Agent>(json, _options);
        return (agent, agentFile);
    }

    private string? FetchInstruction(string fileDir)
    {
        var file = Path.Combine(fileDir, $"{AGENT_INSTRUCTION_FILE}.{_agentSettings.TemplateFormat}");
        if (!File.Exists(file)) return null;

        var instruction = File.ReadAllText(file);
        return instruction;
    }

    private List<FunctionDef> FetchFunctions(string fileDir)
    {
        var file = Path.Combine(fileDir, AGENT_FUNCTIONS_FILE);
        if (!File.Exists(file)) return new List<FunctionDef>();

        var functionsJson = File.ReadAllText(file);
        var functions = JsonSerializer.Deserialize<List<FunctionDef>>(functionsJson, _options);
        return functions;
    }

    private List<string> FetchSamples(string fileDir)
    {
        var file = Path.Combine(fileDir, AGENT_SAMPLES_FILE);
        if (!File.Exists(file)) return new List<string>();

        return File.ReadAllLines(file)?.ToList() ?? new List<string>();
    }

    private List<AgentTemplate> FetchTemplates(string fileDir)
    {
        var templates = new List<AgentTemplate>();
        var templateDir = Path.Combine(fileDir, "templates");
        if (!Directory.Exists(templateDir)) return templates;

        foreach (var file in Directory.GetFiles(templateDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var name = string.Join('.', splits.Take(splits.Length - 1));
            var extension = splits.Last();
            if (extension.Equals(_agentSettings.TemplateFormat, StringComparison.OrdinalIgnoreCase))
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

    private string[] ParseFileNameByPath(string path, string separator = ".")
    {
        var name = path.Split(Path.DirectorySeparatorChar).Last();
        return name.Split(separator);
    }
    #endregion
}
