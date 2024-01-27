using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Loggers.Models;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class StreamingLogHook : ConversationHookBase, IContentGeneratingHook
{
    private readonly ConversationSetting _convSettings;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IConversationStateService _state;
    private readonly IUserIdentity _user;

    public StreamingLogHook(
        ConversationSetting convSettings,
        IServiceProvider serivces,
        IHubContext<SignalRHub> chatHub,
        IConversationStateService state,
        IUserIdentity user)
    {
        _convSettings = convSettings;
        _services = serivces;
        _chatHub = chatHub;
        _state = state;
        _user = user;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };
    }
    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        var log = $"MessageId: {message.MessageId} ==>\r\n{message.Role}: {message.Content}";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, _user.UserName, log));
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!_convSettings.ShowVerboseLog) return;

        /*var _state = _services.GetRequiredService<IConversationStateService>();
        var conversationId = _state.GetConversationId();
        var dialog = conversations.Last();
        var log = $"{dialog.Role}: {dialog.Content} [msg_id: {dialog.MessageId}] ==>";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, log));*/
    }

    public override async Task OnFunctionExecuted(RoleDialogModel message)
    {
        
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var agentService = _services.GetRequiredService<IAgentService>();
        var conversationId = _state.GetConversationId();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        await _chatHub.Clients.User(_user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, agent?.Name, tokenStats.Prompt));

        var log = message.Role == AgentRole.Function ?
                $"[{agent?.Name}]: {message.FunctionName}({message.FunctionArgs})" :
                $"[{agent?.Name}]: {message.Content}";
        log += $"\r\n<== MessageId: {message.MessageId}";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, agent?.Name, log));
    }

    private string BuildLog(string conversationId, string? name, string content)
    {
        var log = new StreamingLogModel
        {
            ConversationId = conversationId,
            Name = name,
            Content = content,
            CreateTime = DateTime.UtcNow
        };
        return JsonSerializer.Serialize(log, _serializerOptions);
    }
}
