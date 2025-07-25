using BotSharp.Abstraction.Evaluations;

namespace BotSharp.Core.Evaluations;

public class EvaluationConversationHook : ConversationHookBase
{
    private readonly IExecutionLogger _logger;
    private readonly ConversationSetting _convSettings;

    public EvaluationConversationHook(IExecutionLogger logger, ConversationSetting convSettings)
    {
        _logger = logger;
        _convSettings = convSettings;
    }

    public override Task OnMessageReceived(RoleDialogModel message)
    {
        if (Conversation != null && _convSettings.EnableExecutionLog)
        {
            _logger.Append(Conversation.Id, $"[{DateTime.Now}] {message.Role}: {message.Content}");
        }
        return base.OnMessageReceived(message);
    }

    public override Task OnFunctionExecuted(RoleDialogModel message, string from = InvokeSource.Manual)
    {
        if (Conversation != null && _convSettings.EnableExecutionLog)
        {
            _logger.Append(Conversation.Id, $"[{DateTime.Now}] {message.Role}: {message.FunctionName}({message.FunctionArgs}) => {message.Content}");
        } 
        return base.OnFunctionExecuted(message, from: from);
    }

    public override Task OnResponseGenerated(RoleDialogModel message)
    {
        if (Conversation != null && _convSettings.EnableExecutionLog)
        {
            _logger.Append(Conversation.Id, $"[{DateTime.Now}] {message.Role}: {message.Content}");
        }
        return base.OnResponseGenerated(message);
    }

    public override Task OnHumanInterventionNeeded(RoleDialogModel message)
    {
        if (Conversation != null && _convSettings.EnableExecutionLog)
        {
            _logger.Append(Conversation.Id, $"[{DateTime.Now}] {AgentRole.Function}: trigger_event({{\"event\": \"{message.FunctionName}\"}})");
        }
        return base.OnHumanInterventionNeeded(message);
    }

    public override Task OnConversationEnding(RoleDialogModel message)
    {
        if (Conversation != null && _convSettings.EnableExecutionLog)
        {
            _logger.Append(Conversation.Id, $"[{DateTime.Now}] {AgentRole.Function}: trigger_event({{\"event\": \"{message.FunctionName}\"}})");
        }
        return base.OnConversationEnding(message);
    }
}
