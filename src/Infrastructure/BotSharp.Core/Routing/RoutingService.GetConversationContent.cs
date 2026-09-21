namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<string> GetConversationContent(List<RoleDialogModel> dialogs, int maxDialogCount = 100)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var conversation = new StringBuilder();
        // A tool result from an earlier turn says nothing about which agent should answer now.
        var conversationDialogs = dialogs
            .Where(x => !x.ExcludeFromContext)
            .Where(x => x.Role != AgentRole.Function || x.MessageId == Context.MessageId)
            .TakeLast(maxDialogCount)
            .ToList();
        foreach (var dialog in conversationDialogs)
        {
            var agent = dialog.Role == AgentRole.User ? null : await agentService.GetAgent(dialog.CurrentAgentId);
            var name = agent?.Name ?? dialog.Role;

            if (dialog.Role == AgentRole.User)
            {
                // What the user said can arrive as a postback payload rather than as text
                conversation.Append($"{name}: {dialog.LlmContent}\r\n");
            }
            else if (dialog.Role == AgentRole.Function)
            {
                // A tool result is not something the agent said, so name the call it answers
                conversation.Append($"{name}: Call function {dialog.FunctionName}({dialog.FunctionArgs}) => {dialog.Content}\r\n");
            }
            else
            {
                // Assistant reply doesn't need help with payload
                conversation.Append($"{name}: {dialog.Content}\r\n");
            }
        }

        return conversation.ToString();
    }
}
