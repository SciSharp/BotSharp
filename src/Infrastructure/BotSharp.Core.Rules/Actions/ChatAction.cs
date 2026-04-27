namespace BotSharp.Core.Rules.Actions;

public class ChatAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ChatAction> _logger;

    public ChatAction(
        IServiceProvider services,
        ILogger<ChatAction> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Name => "send_message_to_agent";

    public FlowUnitSchema? InputSchema => new();

    public FlowUnitSchema? OutputSchema => new(
        properties: new()
        {
            ["agent_id"] = new("string", "The agent ID"),
            ["conversation_id"] = new("string", "The created conversation ID")
        },
        required: ["agent_id", "conversation_id"]
    );

    public async Task<RuleNodeResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
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

            var data = new Dictionary<string, string>(convService.States.GetStates() ?? []);
            await convService.SaveStates();

            _logger.LogInformation("Chat rule action executed successfully for agent {AgentId}, conversation {ConversationId}", agent.Id, conv.Id);


            data["agent_id"] = agent.Id;
            data["conversation_id"] = conv.Id;

            return new RuleNodeResult
            {
                Success = true,
                Response = conv.Id,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when sending chat via rule action for agent {AgentId} and trigger {TriggerName}", agent.Id, trigger.Name);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
