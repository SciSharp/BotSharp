namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<string> GetConversationContent(List<RoleDialogModel> dialogs, int maxDialogCount = 50)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var conversation = "";

        foreach (var dialog in dialogs.TakeLast(maxDialogCount))
        {
            var role = dialog.Role;
            if (role != AgentRole.User)
            {
                var agent = await agentService.GetAgent(dialog.CurrentAgentId);
                role = agent.Name;
            }

            conversation += $"{role}: {dialog.Payload ?? dialog.SecondaryContent ?? dialog.Content}\r\n";
        }

        return conversation;
    }
}
