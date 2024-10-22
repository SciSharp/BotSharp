namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<string> GetConversationContent(List<RoleDialogModel> dialogs, int maxDialogCount = 100)
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

            if (role == AgentRole.User)
            {
                conversation += $"{role}: {dialog.Payload ?? dialog.Content}\r\n";
            }
            else
            {
                // Assistant reply doesn't need help with payload
                conversation += $"{role}: {dialog.Content}\r\n";
            }
        }

        return conversation;
    }
}
