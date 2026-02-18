namespace BotSharp.Core.Rules.Actions;

public sealed class ChatRuleAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ChatRuleAction> _logger;

    public ChatRuleAction(
        IServiceProvider services,
        ILogger<ChatRuleAction> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Name => "send_message_to_agent";

    public async Task<RuleActionResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleActionContext context)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            var channel = trigger.Channel;
            var convService = sp.GetRequiredService<IConversationService>();
            var conv = await convService.NewConversation(new Conversation
            {
                Channel = channel,
                Title = context.Text,
                AgentId = agent.Id
            });

            var message = new RoleDialogModel(AgentRole.User, context.Text);

            var allStates = new List<MessageState>
            {
                new("channel", channel)
            };

            if (!context.Parameters.IsNullOrEmpty())
            {
                var states = context.Parameters.Where(x => x.Value != null).Select(x => new MessageState(x.Key, x.Value!));
                allStates.AddRange(states);
            }

            await convService.SetConversationId(conv.Id, allStates);
            await convService.SendMessage(agent.Id,
                message,
                null,
                msg => Task.CompletedTask);

            await convService.SaveStates();

            _logger.LogInformation("Chat rule action executed successfully for agent {AgentId}, conversation {ConversationId}", agent.Id, conv.Id);

            return new RuleActionResult
            {
                Success = true,
                Response = conv.Id,
                Data = new()
                {
                    ["agent_id"] = agent.Id,
                    ["conversation_id"] = conv.Id
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when sending chat via rule action for agent {AgentId} and trigger {TriggerName}", agent.Id, trigger.Name);
            return RuleActionResult.Failed(ex.Message);
        }
    }
}
