using System.IO;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private ConversationState _states;
    private MyDatabaseSettings _dbSettings;
    private string _conversationId;
    private string _file;

    public ConversationStateService(ILogger<ConversationStateService> logger,
        IServiceProvider services, 
        MyDatabaseSettings dbSettings)
    {
        _logger = logger;
        _services = services;
        _dbSettings = dbSettings;
        _states = new ConversationState();
    }

    public void SetState(string name, string value)
    {
        var hooks = _services.GetServices<IConversationHook>();
        string preValue = _states.ContainsKey(name) ? _states[name] : "";
        if (!_states.ContainsKey(name) || _states[name] != value)
        {
            var currentValue = value;
            _states[name] = currentValue;
            _logger.LogInformation($"Set state: {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }
        }
    }

    public ConversationState Load(string conversationId)
    {
        _conversationId = conversationId;

        _file = GetStorageFile(_conversationId);

        if (File.Exists(_file))
        {
            var dict = File.ReadAllLines(_file);
            foreach (var line in dict)
            {
                _states[line.Split('=')[0]] = line.Split('=')[1];
                _logger.LogInformation($"Loaded state: {line}");
            }
        }

        _logger.LogInformation($"Loaded conversation states: {_conversationId}");
        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateLoaded(_states).Wait();
        }

        return _states;
    }

    public void Save()
    {
        if (_conversationId == null)
        {
            return;
        }

        var states = new List<string>();
        
        foreach (var dic in _states)
        {
            states.Add($"{dic.Key}={dic.Value}");
        }
        File.WriteAllLines(_file, states);
        _logger.LogInformation($"Saved state {_conversationId}");
    }

    public void CleanState()
    {
        File.Delete(_file);
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "state.dict");
    }

    public ConversationState GetStates()
        => _states;

    public string GetState(string name)
    {
        if (!_states.ContainsKey(name))
        {
            _states[name] = "";
        }
        return _states[name];
    }

    public void Dispose()
    {
        Save();
    }
}
