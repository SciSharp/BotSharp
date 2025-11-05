using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Routing.Models;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class StreamingLogHook : ConversationHookBase, IContentGeneratingHook, IRoutingHook
{
    private readonly ConversationSetting _convSettings;
    private readonly BotSharpOptions _options;
    private readonly JsonSerializerOptions _localJsonOptions;
    private readonly ChatHubSettings _settings;
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly ILogger<StreamingLogHook> _logger;
    private readonly IConversationStateService _state;
    private readonly IUserIdentity _user;
    private readonly IAgentService _agentService;
    private readonly IRoutingContext _routingCtx;

    public StreamingLogHook(
        ConversationSetting convSettings,
        BotSharpOptions options,
        ChatHubSettings settings,
        IServiceProvider serivces,
        IHubContext<SignalRHub> chatHub,
        ILogger<StreamingLogHook> logger,
        IConversationStateService state,
        IUserIdentity user,
        IAgentService agentService,
        IRoutingContext routingCtx)
    {
        _convSettings = convSettings;
        _options = options;
        _settings = settings;
        _services = serivces;
        _chatHub = chatHub;
        _logger = logger;
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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"{GetMessageContent(message)}";
        var replyContent = JsonSerializer.Serialize(replyMsg, _options.JsonSerializerOptions);
        log += $"\r\n\r\n```json\r\n{replyContent}\r\n```";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = _user.UserName,
            Source = ContentLogSource.UserInput,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task OnSessionUpdated(Agent agent, string instruction, FunctionDef[] functions, bool isInit = false)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        // Agent queue log
        var log = $"{instruction}";
        if (functions.Length > 0)
        {
            log += $"\r\n\r\n[FUNCTIONS]:\r\n\r\n{string.Join("\r\n\r\n", functions.Select(x => JsonSerializer.Serialize(x, BotSharpOptions.defaultJsonOptions)))}";
        }
        _logger.LogInformation(log);

        if (isInit) return;

        var message = new RoleDialogModel(AgentRole.Assistant, log)
        {
            MessageId = _routingCtx.MessageId
        };
        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent.Name,
            AgentId = agent.Id,
            Source = ContentLogSource.Prompt,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!_convSettings.ShowVerboseLog) return;
    }

    public override async Task OnFunctionExecuting(RoleDialogModel message, InvokeFunctionOptions? options = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        if (message.FunctionName == "route_to_agent") return;

        var agent = await _agentService.GetAgent(message.CurrentAgentId);
        message.FunctionArgs = message.FunctionArgs ?? "{}";
        var args = message.FunctionArgs.FormatJson();
        var log = $"*{message.Indication.Replace("\r", string.Empty).Replace("\n", string.Empty)}* \r\n\r\n **{message.FunctionName}**()";
        log += args.Length > 5 ? $" \r\n```json\r\n{args}\r\n```" : string.Empty;

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public override async Task OnFunctionExecuted(RoleDialogModel message, InvokeFunctionOptions? options = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        if (message.FunctionName == "route_to_agent") return;

        var agent = await _agentService.GetAgent(message.CurrentAgentId);
        message.FunctionArgs = message.FunctionArgs ?? "{}";
        var log = $"{message.FunctionName} =>\r\n*{message.Content?.Trim()}*";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
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

        var agent = await _agentService.GetAgent(message.CurrentAgentId);

        var log = tokenStats.Prompt;

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.Prompt,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
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
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var stateLog = BuildStateLog(conv.ConversationId, routingCtx.EntryAgentId, _state.GetStates(), message);
        await SendEvent(ChatEvent.OnConversateStateLogGenerated, conv.ConversationId, stateLog);

        if (message.Role == AgentRole.Assistant)
        {
            var agent = await _agentService.GetAgent(message.CurrentAgentId);
            var log = $"{GetMessageContent(message)}";
            if (message.RichContent != null || message.SecondaryRichContent != null)
            {
                var richContent = JsonSerializer.Serialize(message.SecondaryRichContent ?? message.RichContent, _localJsonOptions);
                log += $"\r\n\r\n```json\r\n{richContent}\r\n```";
            }

            var input = new ContentLogInputModel(conv.ConversationId, message)
            {
                Name = agent?.Name,
                AgentId = agent?.Id,
                Source = ContentLogSource.AgentResponse,
                Log = log
            };
            await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
        }
    }

    public override async Task OnTaskCompleted(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"{GetMessageContent(message)}";
        var agent = await _agentService.GetAgent(message.CurrentAgentId);

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent.Name,
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public override async Task OnConversationEnding(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var log = $"Conversation ended";
        var agent = await _agentService.GetAgent(message.CurrentAgentId);

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name ?? "System",
            Source = ContentLogSource.FunctionCall,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
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
        var agent = await _agentService.GetAgent(agentId);

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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public override async Task OnStateChanged(StateChangeModel stateChange)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        if (stateChange == null) return;

        await SendEvent(ChatEvent.OnStateChangeGenerated, conversationId, BuildStateChangeLog(stateChange));
    }
    #endregion

    #region IRoutingHook
    public async Task OnAgentEnqueued(string agentId, string preAgentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.GetAgent(agentId);

        // Agent queue log
        var log = $"{agent.Name} is enqueued";
        await SendEvent(ChatEvent.OnAgentQueueChanged, conversationId, BuildAgentQueueChangedLog(conversationId, log));

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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task OnAgentDequeued(string agentId, string currentAgentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.GetAgent(agentId);
        var currentAgent = await _agentService.GetAgent(currentAgentId);

        // Agent queue log
        var log = $"{agent.Name} is dequeued";
        await SendEvent(ChatEvent.OnAgentQueueChanged, conversationId, BuildAgentQueueChangedLog(conversationId, log));

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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task OnAgentReplaced(string fromAgentId, string toAgentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var fromAgent = await _agentService.GetAgent(fromAgentId);
        var toAgent = await _agentService.GetAgent(toAgentId);

        // Agent queue log
        var log = $"Agent queue is replaced from {fromAgent.Name} to {toAgent.Name}";
        await SendEvent(ChatEvent.OnAgentQueueChanged, conversationId, BuildAgentQueueChangedLog(conversationId, log));

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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task OnAgentQueueEmptied(string agentId, string? reason = null)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        // Agent queue log
        var log = $"Agent queue is empty";
        await SendEvent(ChatEvent.OnAgentQueueChanged, conversationId, BuildAgentQueueChangedLog(conversationId, log));

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
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task OnRoutingInstructionReceived(FunctionCallFromLlm instruct, RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.GetAgent(message.CurrentAgentId);
        var log = JsonSerializer.Serialize(instruct, _options.JsonSerializerOptions);
        log = $"```json\r\n{log}\r\n```";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.AgentResponse,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }

    public async Task OnRoutingInstructionRevised(FunctionCallFromLlm instruct, RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        if (string.IsNullOrEmpty(conversationId)) return;

        var agent = await _agentService.GetAgent(message.CurrentAgentId);
        var log = $"Revised user goal agent to {instruct.OriginalAgent}";

        var input = new ContentLogInputModel(conversationId, message)
        {
            Name = agent?.Name,
            AgentId = agent?.Id,
            Source = ContentLogSource.HardRule,
            Log = log
        };
        await SendEvent(ChatEvent.OnConversationContentLogGenerated, conversationId, BuildContentLog(input));
    }
    #endregion


    #region Private methods
    private async Task SendEvent<T>(string @event, string conversationId, T data, [CallerMemberName] string callerName = "")
    {
        var user = _services.GetRequiredService<IUserIdentity>();
        var json = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        await EventEmitter.SendChatEvent(_services, _logger, @event, conversationId, user?.Id, json, nameof(StreamingLogHook), callerName);
    }

    private ContentLogOutputModel BuildContentLog(ContentLogInputModel input)
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
            CreatedTime = DateTime.UtcNow
        };

        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (convSettings.EnableContentLog)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.SaveConversationContentLog(output);
        }

        return output;
    }

    private ConversationStateLogModel BuildStateLog(string conversationId, string agentId, Dictionary<string, string> states, RoleDialogModel message)
    {
        var log = new ConversationStateLogModel
        {
            ConversationId = conversationId,
            AgentId = agentId,
            MessageId = message.MessageId,
            States = states,
            CreatedTime = DateTime.UtcNow
        };

        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (convSettings.EnableStateLog)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.SaveConversationStateLog(log);
        }

        return log;
    }

    private StateChangeOutputModel BuildStateChangeLog(StateChangeModel stateChange)
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

        return log;
    }

    private AgentQueueChangedLogModel BuildAgentQueueChangedLog(string conversationId, string log)
    {
        var model = new AgentQueueChangedLogModel
        {
            ConversationId = conversationId,
            Log = log,
            CreatedTime = DateTime.UtcNow
        };

        return model;
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
