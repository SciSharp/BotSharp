using System.IO;
using FunctionDef = BotSharp.Abstraction.Functions.Models.FunctionDef;
using BotSharp.Abstraction.Users.Models;
using System.Text.Encodings.Web;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Statistics.Settings;
using BotSharp.Abstraction.Tasks.Models;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Repository;

public partial class FileRepository : IBotSharpRepository
{
    private readonly IServiceProvider _services;
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly ConversationSetting _conversationSettings;
    private readonly StatisticsSettings _statisticsSetting;
    private readonly ILogger<FileRepository> _logger;
    private JsonSerializerOptions _options;

    private const string AGENT_FILE = "agent.json";
    private const string AGENT_INSTRUCTION_FILE = "instruction";
    private const string AGENT_SAMPLES_FILE = "samples.txt";
    private const string USER_FILE = "user.json";
    private const string USER_AGENT_FILE = "agents.json";
    private const string CONVERSATION_FILE = "conversation.json";
    private const string STATS_FILE = "stats.json";
    private const string DIALOG_FILE = "dialogs.json";
    private const string STATE_FILE = "state.json";
    private const string BREAKPOINT_FILE = "breakpoint.json";
    private const string EXECUTION_LOG_FILE = "execution.log";
    private const string PLUGIN_CONFIG_FILE = "config.json";
    private const string AGENT_TASK_PREFIX = "#metadata";
    private const string AGENT_TASK_SUFFIX = "/metadata";
    private const string TRANSLATION_MEMORY_FILE = "memory.json";
    private const string AGENT_INSTRUCTIONS_FOLDER = "instructions";
    private const string AGENT_FUNCTIONS_FOLDER = "functions";
    private const string AGENT_TEMPLATES_FOLDER = "templates";
    private const string AGENT_RESPONSES_FOLDER = "responses";
    private const string AGENT_TASKS_FOLDER = "tasks";
    private const string USERS_FOLDER = "users";
    private const string KNOWLEDGE_FOLDER = "knowledgebase";
    private const string VECTOR_FOLDER = "vector";
    private const string COLLECTION_CONFIG_FILE = "collection-config.json";
    private const string KNOWLEDGE_DOC_FOLDER = "document";
    private const string KNOWLEDGE_DOC_META_FILE = "meta.json";

    public FileRepository(
        IServiceProvider services,
        BotSharpDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        ConversationSetting conversationSettings,
        StatisticsSettings statisticsSettings,
         ILogger<FileRepository> logger)
    {
        _services = services;
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _conversationSettings = conversationSettings;
        _statisticsSetting = statisticsSettings;
        _logger = logger;

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

            var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER);
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
                        var (defaultInstruction, channelInstructions) = FetchInstructions(d);
                        agent = agent.SetInstruction(defaultInstruction)
                                     .SetChannelInstructions(channelInstructions)
                                     .SetFunctions(FetchFunctions(d))
                                     .SetTemplates(FetchTemplates(d))
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

            var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER);
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
    private void DeleteBeforeCreateDirectory(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return;

        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        Directory.CreateDirectory(dir);
    }

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

    private (string, List<ChannelInstruction>) FetchInstructions(string fileDir)
    {
        var defaultInstruction = string.Empty;
        var channelInstructions = new List<ChannelInstruction>();

        var instructionDir = Path.Combine(fileDir, AGENT_INSTRUCTIONS_FOLDER);
        if (!Directory.Exists(instructionDir))
        {
            return (defaultInstruction, channelInstructions);
        }

        foreach (var file in Directory.GetFiles(instructionDir))
        {
            var extension = Path.GetExtension(file).Substring(1);
            if (!extension.IsEqualTo(_agentSettings.TemplateFormat))
            {
                continue;
            }

            var segments = Path.GetFileName(file).Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (segments.IsNullOrEmpty() || !segments[0].IsEqualTo(AGENT_INSTRUCTION_FILE))
            {
                continue;
            }

            if (segments.Length == 2)
            {
                defaultInstruction = File.ReadAllText(file);
            }
            else if (segments.Length == 3)
            {
                var item = new ChannelInstruction
                {
                    Channel = segments[1],
                    Instruction = File.ReadAllText(file)
                };
                channelInstructions.Add(item);
            }
        }
        return (defaultInstruction, channelInstructions);
    }

    private List<FunctionDef> FetchFunctions(string fileDir)
    {
        var functions = new List<FunctionDef>();
        var functionDir = Path.Combine(fileDir, AGENT_FUNCTIONS_FOLDER);

        if (!Directory.Exists(functionDir)) return functions;

        foreach ( var file in Directory.GetFiles(functionDir))
        {
            try
            {
                var extension = Path.GetExtension(file).Substring(1);
                if (extension != "json") continue;
                
                var json = File.ReadAllText(file);
                var function = JsonSerializer.Deserialize<FunctionDef>(json, _options);
                functions.Add(function);
            }
            catch
            {
                continue;
            }
            
        }
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
        var templateDir = Path.Combine(fileDir, AGENT_TEMPLATES_FOLDER);
        if (!Directory.Exists(templateDir)) return templates;

        foreach (var file in Directory.GetFiles(templateDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splitIdx = fileName.LastIndexOf(".");
            var name = fileName.Substring(0, splitIdx);
            var extension = fileName.Substring(splitIdx + 1);
            if (extension.Equals(_agentSettings.TemplateFormat, StringComparison.OrdinalIgnoreCase))
            {
                var content = File.ReadAllText(file);
                templates.Add(new AgentTemplate(name, content));
            }
        }

        return templates;
    }

    private List<AgentTask> FetchTasks(string fileDir)
    {
        var tasks = new List<AgentTask>();
        var taskDir = Path.Combine(fileDir, AGENT_TASKS_FOLDER);
        if (!Directory.Exists(taskDir)) return tasks;

        foreach (var file in Directory.GetFiles(taskDir))
        {
            var task = ParseAgentTask(file);
            if (task == null) continue;

            tasks.Add(task);
        }

        return tasks;
    }

    private List<AgentResponse> FetchResponses(string fileDir)
    {
        var responses = new List<AgentResponse>();
        var responseDir = Path.Combine(fileDir, AGENT_RESPONSES_FOLDER);
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

    private Agent? ParseAgent(string agentDir)
    {
        if (string.IsNullOrEmpty(agentDir)) return null;

        var agentJson = File.ReadAllText(Path.Combine(agentDir, AGENT_FILE));
        if (string.IsNullOrEmpty(agentJson)) return null;

        var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
        if (agent == null) return null;

        var (defaultInstruction, channelInstructions) = FetchInstructions(agentDir);
        var functions = FetchFunctions(agentDir);
        var samples = FetchSamples(agentDir);
        var templates = FetchTemplates(agentDir);
        var responses = FetchResponses(agentDir);

        return agent.SetInstruction(defaultInstruction)
                    .SetChannelInstructions(channelInstructions)
                    .SetFunctions(functions)
                    .SetTemplates(templates)
                    .SetSamples(samples)
                    .SetResponses(responses);
    }

    private AgentTask? ParseAgentTask(string taskFile)
    {
        if (string.IsNullOrWhiteSpace(taskFile)) return null;

        var fileName = taskFile.Split(Path.DirectorySeparatorChar).Last();
        var id = fileName.Split('.').First();
        var data = File.ReadAllText(taskFile);
        var pattern = $@"{AGENT_TASK_PREFIX}.+{AGENT_TASK_SUFFIX}";
        var metaData = Regex.Match(data, pattern, RegexOptions.Singleline);

        if (!metaData.Success) return null;

        var task = metaData.Value.JsonContent<AgentTask>();
        if (task == null) return null;

        task.Id = id;
        pattern = $@"{AGENT_TASK_SUFFIX}.+";
        var content = Regex.Match(data, pattern, RegexOptions.Singleline).Value;
        task.Content = content.Substring(AGENT_TASK_SUFFIX.Length).Trim();
        return task;
    }

    private string[] ParseFileNameByPath(string path, string separator = ".")
    {
        var name = path.Split(Path.DirectorySeparatorChar).Last();
        return name.Split(separator);
    }
    #endregion
}
