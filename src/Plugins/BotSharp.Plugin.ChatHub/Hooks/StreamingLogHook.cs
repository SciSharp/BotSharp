using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Loggers.Models;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class StreamingLogHook : IContentGeneratingHook
{
    private readonly ConversationSetting _convSettings;
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly JsonSerializerOptions _serializerOptions;

    public StreamingLogHook(
        ConversationSetting convSettings,
        IServiceProvider serivces,
        IHubContext<SignalRHub> chatHub)
    {
        _convSettings = convSettings;
        _services = serivces;
        _chatHub = chatHub;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var user = _services.GetRequiredService<IUserIdentity>();
        var states = _services.GetRequiredService<IConversationStateService>();
        var conversationId = states.GetConversationId();
        var dialog = conversations.Last();
        var log = $"{dialog.Role}: {dialog.Content} [msg_id: {dialog.MessageId}] ==>";
        await _chatHub.Clients.User(user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, log));
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var agentService = _services.GetRequiredService<IAgentService>();
        var states = _services.GetRequiredService<IConversationStateService>();
        var conversationId = states.GetConversationId();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        var log = message.Role == AgentRole.Function ?
                $"[{agent?.Name}]: {message.FunctionName}({message.FunctionArgs})" :
                $"[{agent?.Name}]: {message.Content}" + $" <== [msg_id: {message.MessageId}]";

        var user = _services.GetRequiredService<IUserIdentity>();
        await _chatHub.Clients.User(user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, tokenStats.Prompt));
        await _chatHub.Clients.User(user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, log));
    }

    private string BuildLog(string conversationId, string content)
    {
        var log = new StreamingLogModel
        {
            ConversationId = conversationId,
            Content = content,
            CreateTime = DateTime.UtcNow
        };
        return JsonSerializer.Serialize(log, _serializerOptions);
    }
}
