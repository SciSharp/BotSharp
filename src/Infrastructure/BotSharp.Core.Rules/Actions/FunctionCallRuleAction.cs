using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Rules.Actions;

public sealed class FunctionCallRuleAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FunctionCallRuleAction> _logger;

    public FunctionCallRuleAction(
        IServiceProvider services,
        ILogger<FunctionCallRuleAction> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Name => "function_call";

    public async Task<RuleActionResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleActionContext context)
    {
        var funcName = context.Parameters.TryGetValueOrDefault("function_name", string.Empty);
        var func = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name.IsEqualTo(funcName));

        if (func == null)
        {
            var errorMsg = $"Unable to find function '{funcName}' when running action {agent.Name}-{trigger.Name}";
            _logger.LogWarning(errorMsg);
            return RuleActionResult.Failed(errorMsg);
        }

        var funcArg = context.Parameters.TryGetValueOrDefault<RoleDialogModel>("function_argument") ?? new();
        await func.Execute(funcArg);

        return new RuleActionResult
        {
            Success = true,
            Response = funcArg?.RichContent?.Message?.Text ?? funcArg?.Content
        };
    }
}
