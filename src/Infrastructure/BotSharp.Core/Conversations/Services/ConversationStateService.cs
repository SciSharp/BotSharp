using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private ConversationState _state;
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
    }

    public void SetState(string name, string value)
    {
        var hooks = _services.GetServices<IConversationHook>();
        string preValue = _state.ContainsKey(name) ? _state[name] : "";
        if (!_state.ContainsKey(name) || _state[name] != value)
        {
            var currentValue = value;
            _state[name] = currentValue;
            _logger.LogInformation($"Set state: {name} = {value}");
            foreach (var hook in hooks)
            {
                hook.OnStateChanged(name, preValue, currentValue).Wait();
            }
        }
    }

    public void SetConversation(string conversationId)
    {
        _conversationId = conversationId;
    }

    public ConversationState Load()
    {
        if (_state != null)
        {
            return _state;
        }

        _state = new ConversationState();

        _file = GetStorageFile(_conversationId);

        if (File.Exists(_file))
        {
            var dict = File.ReadAllLines(_file);
            foreach (var line in dict)
            {
                _state[line.Split('=')[0]] = line.Split('=')[1];
            }
        }

        _logger.LogInformation($"Loaded state {_conversationId}");
        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            hook.OnStateLoaded(_state).Wait();
        }

        return _state;
    }

    public void Save()
    {
        var states = new List<string>();
        
        foreach (var dic in _state)
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

    public string GetState(string name)
    {
        if (!_state.ContainsKey(name))
        {
            _state[name] = "";
        }
        return _state[name];
    }

    public void Dispose()
    {
        Save();
    }
}
