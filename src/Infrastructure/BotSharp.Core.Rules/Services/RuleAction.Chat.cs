namespace BotSharp.Core.Rules.Services;

public partial class RuleAction : IRuleAction
{
    public async Task<string> SendChatAsync(Agent agent, RuleChatActionPayload payload)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conv = await convService.NewConversation(new Conversation
        {
            Channel = payload.Channel,
            Title = payload.Text,
            AgentId = agent.Id
        });

        var message = new RoleDialogModel(AgentRole.User, payload.Text);

        var allStates = new List<MessageState>
        {
            new("channel", payload.Channel)
        };

        if (!payload.States.IsNullOrEmpty())
        {
            allStates.AddRange(payload.States!);
        }

        await convService.SetConversationId(conv.Id, allStates);
        await convService.SendMessage(agent.Id,
            message,
            null,
            msg => Task.CompletedTask);

        await convService.SaveStates();
        return conv.Id;
    }
}
