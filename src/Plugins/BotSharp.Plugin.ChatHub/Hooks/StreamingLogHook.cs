using Microsoft.AspNetCore.SignalR;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class StreamingLogHook : ConversationHookBase, IContentGeneratingHook, IRoutingHook
{
    private readonly ConversationSetting _convSettings;
    private readonly BotSharpOptions _options;
    private readonly JsonSerializerOptions _localJsonOptions;
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IConversationStateService _state;
    private readonly IUserIdentity _user;
    private readonly IAgentService _agentService;
    private readonly IRoutingContext _routingCtx;

    #region Event
    private const string CONTENT_LOG_GENERATED = "OnConversationContentLogGenerated";
    private const string STATE_LOG_GENERATED = "OnConversateStateLogGenerated";
    private const string AGENT_QUEUE_CHANGED = "OnAgentQueueChanged";
    private const string STATE_CHANGED = "OnStateChangeGenerated";
    #endregion

    public StreamingLogHook(
        ConversationSetting convSettings,
        BotSharpOptions options,
        IServiceProvider serivces,
        IHubContext<SignalRHub> chatHub,
        IConversationStateService state,
        IUserIdentity user,
        IAgentService agentService,
        IRoutingContext routingCtx)
    {
        _convSettings = convSettings;
        _options = options;
        _services = serivces;
        _chatHub = chatHub;
        _state = state;
        _user = user;
        _agentService = agentService;
        _routingCtx = routingCtx;
        _localJsonOptions = InitLocalJsonOptions(options);
    }

    #region IConversationHook
    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"{GetMessageContent(message)}";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = _user.UserName,
            Source = ContentLogSource.UserInput,
            Log = log
        };
        await SendContentLog(input);
    }

    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"{GetMessageContent(message)}";
        var replyContent = JsonSerializer.Serialize(replyMsg, _options.JsonSerializerOptions);
        log += $"\r\n```json\r\n{replyContent}\r\n```";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = _user.UserName,
            Source = ContentLogSource.UserInput,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task OnRenderingTemplate(Agent agent, string name, string content)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"{agent.Name} is using template {name}";
        var message = new RoleDialogModel(AgentRole.System, log)
        {
            MessageId = _routingCtx.MessageId
        };

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent.Name,
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!_convSettings.ShowVerboseLog) return;
    }

    public override async Task OnFunctionExecuting(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        if (message.FunctionName == "route_to_agent") return;

        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        message.FunctionArgs = message.FunctionArgs ?? "{}";
        var args = message.FunctionArgs.FormatJson();
        var log = $"{message.FunctionName} <u>executing</u>\r\n```json\r\n{args}\r\n```";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendContentLog(input);
    }

    public override async Task OnFunctionExecuted(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        if (message.FunctionName == "route_to_agent") return;

        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        message.FunctionArgs = message.FunctionArgs ?? "{}";
        var log = $"{message.FunctionName} =>\r\n*{message.Content?.Trim()}*";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendContentLog(input);
    }

    /// <summary>
    /// Used to log prompt
    /// </summary>
    /// <param name="message"></param>
    /// <param name="tokenStats"></param>
    /// <returns></returns>
    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.LoadAgent(message.CurrentAgentId);

        var log = tokenStats.Prompt;

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.Prompt,
            Log = log
        };
        await SendContentLog(input);
    }

    /// <summary>
    /// Used to log final response
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var conv = _services.GetRequiredService<IConversationService>();
        await SendStateLog(conv.ConversationId, _state.GetStates(), message);

        if (message.Role == AgentRole.Assistant)
        {
            var agent = await _agentService.LoadAgent(message.CurrentAgentId);
            var log = $"{GetMessageContent(message)}";
            if (message.RichContent != null || message.SecondaryRichContent != null)
            {
                var richContent = JsonSerializer.Serialize(message.SecondaryRichContent ?? message.RichContent, _localJsonOptions);
                log += $"\r\n```json\r\n{richContent}\r\n```";
            }

            var input = new ContentLogInputModel(conv.ConversationId, message)
            {
                Name = agent?.Name,
                AgentId = agent?.Id,
                Source = ContentLogSource.AgentResponse,
                Log = log
            };
            await SendContentLog(input);
        }
    }

    public override async Task OnTaskCompleted(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"{GetMessageContent(message)}";
        var agent = await _agentService.LoadAgent(message.CurrentAgentId);

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent.Name,
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendContentLog(input);
    }

    public override async Task OnConversationEnding(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"Conversation ended";
        var agent = await _agentService.LoadAgent(message.CurrentAgentId);

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name ?? "System",
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendContentLog(input);
    }

    public override async Task OnBreakpointUpdated(string conversationId, bool resetStates)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"Conversation breakpoint is updated";
        if (resetStates)
        {
            log += ", states are reset";
        }
        var routing = _services.GetRequiredService<IRoutingService>();
        var agentId = routing.Context.GetCurrentAgentId();
        var agent = await _agentService.LoadAgent(agentId);

        var input = new ContentLogInputModel()
        {
            Name = agent.Name,
            AgentId = agentId,
            ConversationId = conversationId,
            Source = ContentLogSource.HardRule,
            Message = new RoleDialogModel(AgentRole.Assistant, "OnBreakpointUpdated")
            {
                MessageId = _routingCtx.MessageId
            },
            Log = log
        };
        await SendContentLog(input);
    }

    public override async Task OnStateChanged(StateChangeModel stateChange)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        if (stateChange == null) return;

        await SendStateChange(stateChange);
    }
    #endregion

    #region IRoutingHook
    public async Task OnAgentEnqueued(string agentId, string preAgentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.LoadAgent(agentId);

        // Agent queue log
        var log = $"{agent.Name} is enqueued";
        await SendAgentQueueLog(conversationId, log);

        // Content log
        log = $"{agent.Name} is enqueued{(reason != null ? $" ({reason})" : "")}";
        var message = new RoleDialogModel(AgentRole.System, log)
        {
            MessageId = _routingCtx.MessageId
        };

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = "Router",
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task OnAgentDequeued(string agentId, string currentAgentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.LoadAgent(agentId);
        var currentAgent = await _agentService.LoadAgent(currentAgentId);

        // Agent queue log
        var log = $"{agent.Name} is dequeued";
        await SendAgentQueueLog(conversationId, log);

        // Content log
        log = $"{agent.Name} is dequeued{(reason != null ? $" ({reason})" : "")}, current agent is {currentAgent?.Name}";
        var message = new RoleDialogModel(AgentRole.System, log)
        {
            MessageId = _routingCtx.MessageId
        };

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = "Router",
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task OnAgentReplaced(string fromAgentId, string toAgentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var fromAgent = await _agentService.LoadAgent(fromAgentId);
        var toAgent = await _agentService.LoadAgent(toAgentId);

        // Agent queue log
        var log = $"Agent queue is replaced from {fromAgent.Name} to {toAgent.Name}";
        await SendAgentQueueLog(conversationId, log);

        // Content log
        log = $"{fromAgent.Name} is replaced to {toAgent.Name}{(reason != null ? $" ({reason})" : "")}";
        var message = new RoleDialogModel(AgentRole.System, log)
        {
            MessageId = _routingCtx.MessageId
        };

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = "Router",
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task OnAgentQueueEmptied(string agentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        // Agent queue log
        var log = $"Agent queue is empty";
        await SendAgentQueueLog(conversationId, log);

        // Content log
        log = reason ?? "Agent queue is cleared";
        var message = new RoleDialogModel(AgentRole.System, log)
        {
            MessageId = _routingCtx.MessageId
        };

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = "Router",
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task OnRoutingInstructionReceived(FunctionCallFromLlm instruct, RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        var log = JsonSerializer.Serialize(instruct, _options.JsonSerializerOptions);
        log = $"```json\r\n{log}\r\n```";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.AgentResponse,
            Log = log
        };
        await SendContentLog(input);
    }

    public async Task OnRoutingInstructionRevised(FunctionCallFromLlm instruct, RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        var log = $"Revised user goal agent to {instruct.OriginalAgent}";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendContentLog(input);
    }
    #endregion


    #region Private methods
    private async Task SendContentLog(ContentLogInputModel input)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(CONTENT_LOG_GENERATED, BuildContentLog(input));
    }

    private async Task SendStateLog(string conversationId, Dictionary<string, string> states, RoleDialogModel message)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(STATE_LOG_GENERATED, BuildStateLog(conversationId, states, message));
    }

    private async Task SendAgentQueueLog(string conversationId, string log)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(AGENT_QUEUE_CHANGED, BuildAgentQueueChangedLog(conversationId, log));
    }

    private async Task SendStateChange(StateChangeModel stateChange)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(STATE_CHANGED, BuildStateChangeLog(stateChange));
    }

    private string BuildContentLog(ContentLogInputModel input)
    {
        var output = new ContentLogOutputModel
        {
            ConversationId = input.ConversationId,
            MessageId = input.Message.MessageId,
            Name = input.Name,
            AgentId = input.AgentId,
            Role = input.Message.Role,
            Content = input.Log,
            Source = input.Source,
            CreateTime = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(output, _options.JsonSerializerOptions);

        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (convSettings.EnableContentLog)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.SaveConversationContentLog(output);
        }

        return json;
    }

    private string BuildStateLog(string conversationId, Dictionary<string, string> states, RoleDialogModel message)
    {
        var log = new ConversationStateLogModel
        {
            ConversationId = conversationId,
            MessageId = message.MessageId,
            States = states,
            CreateTime = DateTime.UtcNow
        };

        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (convSettings.EnableStateLog)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.SaveConversationStateLog(log);
        }

        return JsonSerializer.Serialize(log, _options.JsonSerializerOptions);
    }

    private string BuildStateChangeLog(StateChangeModel stateChange)
    {
        var log = new StateChangeOutputModel
        {
            ConversationId = stateChange.ConversationId,
            MessageId = stateChange.MessageId,
            Name = stateChange.Name,
            BeforeValue = stateChange.BeforeValue,
            BeforeActiveRounds = stateChange.BeforeActiveRounds,
            AfterValue = stateChange.AfterValue,
            AfterActiveRounds = stateChange.AfterActiveRounds,
            DataType = stateChange.DataType,
            Source = stateChange.Source,
            Readonly = stateChange.Readonly,
            CreateTime = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(log, _options.JsonSerializerOptions);
    }

    private string BuildAgentQueueChangedLog(string conversationId, string log)
    {
        var model = new AgentQueueChangedLogModel
        {
            ConversationId = conversationId,
            Log = log,
            CreateTime = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(model, _options.JsonSerializerOptions);
    }

    private string GetMessageContent(RoleDialogModel message)
    {
        return !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content;
    }

    private JsonSerializerOptions InitLocalJsonOptions(BotSharpOptions options)
    {
        var localOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        if (options?.JsonSerializerOptions != null && !options.JsonSerializerOptions.Converters.IsNullOrEmpty())
        {
            foreach (var converter in options.JsonSerializerOptions.Converters)
            {
                localOptions.Converters.Add(converter);
            }
        }

        return localOptions;
    }
    #endregion
}
