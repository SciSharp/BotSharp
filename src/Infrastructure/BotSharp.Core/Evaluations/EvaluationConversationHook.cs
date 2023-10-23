using BotSharp.Abstraction.Evaluations;

namespace BotSharp.Core.Evaluations;

public class EvaluationConversationHook : ConversationHookBase
{
    private readonly IExecutionLogger _logger;

    public EvaluationConversationHook(IExecutionLogger logger)
    {
        _logger = logger;
    }

    public override Task OnMessageReceived(RoleDialogModel message)
    {
        _logger.Append(_conversation.Id, $"[{DateTime.Now}] {message.Role}: {message.Content}");
        return base.OnMessageReceived(message);
    }

    public override Task OnFunctionExecuted(RoleDialogModel message)
    {
        _logger.Append(_conversation.Id, $"[{DateTime.Now}] {message.Role}: {message.FunctionName}({message.FunctionArgs}) => {message.Content}");
        return base.OnFunctionExecuted(message);
    }

    public override Task OnResponseGenerated(RoleDialogModel message)
    {
        _logger.Append(_conversation.Id, $"[{DateTime.Now}] {message.Role}: {message.Content}");
        return base.OnResponseGenerated(message);
    }

    public override Task OnHumanInterventionNeeded(RoleDialogModel message)
    {
        _logger.Append(_conversation.Id, $"[{DateTime.Now}] {AgentRole.Function}: trigger event \"{message.FunctionName}\"");
        return base.OnHumanInterventionNeeded(message);
    }

    public override Task OnConversationEnding(RoleDialogModel message)
    {
        _logger.Append(_conversation.Id, $"[{DateTime.Now}] {AgentRole.Function}: trigger event \"{message.FunctionName}\"");
        return base.OnConversationEnding(message);
    }
}
