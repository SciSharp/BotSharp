using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Users.Models;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class StreamingLogHook : IContentGeneratingHook
{
    private readonly ConversationSetting _convSettings;
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;

    public StreamingLogHook(
        ConversationSetting convSettings,
        IServiceProvider serivces,
        IHubContext<SignalRHub> chatHub)
    {
        _convSettings = convSettings;
        _services = serivces;
        _chatHub = chatHub;
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var user = _services.GetRequiredService<IUserIdentity>();
        var dialog = conversations.Last();
        var log = $"{dialog.Role}: {dialog.Content} [msg_id: {dialog.MessageId}] ==>";
        await _chatHub.Clients.User(user.Id).SendAsync("OnContentLogGenerated", log);
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        var log = message.Role == AgentRole.Function ?
                $"[{agent?.Name}]: {message.FunctionName}({message.FunctionArgs})" :
                $"[{agent?.Name}]: {message.Content}" + $" <== [msg_id: {message.MessageId}]";

        var user = _services.GetRequiredService<IUserIdentity>();
        await _chatHub.Clients.User(user.Id).SendAsync("OnContentLogGenerated", tokenStats.Prompt);
        await _chatHub.Clients.User(user.Id).SendAsync("OnContentLogGenerated", log);
    }
}
