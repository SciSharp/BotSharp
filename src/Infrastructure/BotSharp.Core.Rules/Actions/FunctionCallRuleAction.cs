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

    public async Task<RuleNodeResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        var funcName = context.Parameters.TryGetValue("function_name", out var fName) ? fName : null;
        var func = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name.IsEqualTo(funcName));

        if (func == null)
        {
            var errorMsg = $"Unable to find function '{funcName}' when running action {agent.Name}-{trigger.Name}";
            _logger.LogWarning(errorMsg);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = errorMsg
            };
        }

        var funcArg = context.Parameters.TryGetObjectValueOrDefault<RoleDialogModel>("function_argument", new()) ?? new();
        await func.Execute(funcArg);

        return new RuleNodeResult
        {
            Success = true,
            Response = funcArg?.RichContent?.Message?.Text ?? funcArg?.Content,
            Data = new()
            {
                ["function_name"] = funcName!,
                ["function_argument"] = funcArg?.ConvertToString() ?? "{}",
                ["function_call_result"] = funcArg?.RichContent?.Message?.Text ?? funcArg?.Content ?? string.Empty
            }
        };
    }
}
