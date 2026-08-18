using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Routing.Executor;

namespace BotSharp.Core.Rules.Actions;

public class ToolCallAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ToolCallAction> _logger;

    public ToolCallAction(
        IServiceProvider services,
        ILogger<ToolCallAction> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Name => "tool_call";

    public FlowUnitSchema? InputSchema => new(
        properties: new()
        {
            ["function_name"] = new("string", "The name of the function to call"),
            ["function_argument"] = new("object", "The function argument as a RoleDialogModel JSON")
        },
        required: ["function_name"]
    );

    public FlowUnitSchema? OutputSchema => new(
        properties: new()
        {
            ["function_name"] = new("string", "The executed function name"),
            ["function_argument"] = new("string", "The function argument as JSON string"),
            ["function_call_result"] = new("string", "The function call result text")
        },
        required: ["function_name", "function_argument", "function_call_result"]
    );

    public async Task<RuleNodeResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        var funcName = context.Parameters.TryGetValue("function_name", out var fName) ? fName : null;

        // A missing/blank function_name has to fail gracefully. The old IsEqualTo lookup was
        // null-safe -- it simply matched no callback. Going through the factory instead means null
        // reaches IFunctionExecutorProvider.TryResolve, whose contract declares a non-null string,
        // and some implementations (a mock/blocking provider doing a Dictionary lookup by name, for
        // instance) would throw ArgumentNullException rather than returning Success = false the way
        // this used to. So it is rejected before the factory is touched at all.
        string? canonicalName = null;
        IFunctionExecutor? executor = null;
        if (!string.IsNullOrWhiteSpace(funcName))
        {
            // Registered callbacks are used only to resolve the CANONICAL name, preserving the
            // original case-insensitive semantics; the execution itself must go through the factory,
            // or IFunctionExecutorProvider is bypassed on the rule path.
            canonicalName = _services.GetServices<IFunctionCallback>()
                .FirstOrDefault(x => x.Name.IsEqualTo(funcName))?.Name ?? funcName;
            executor = _services.GetRequiredService<IFunctionExecutorFactory>()
                .Create(canonicalName, agent);
        }

        if (executor == null || canonicalName == null)
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
        await executor.ExecuteAsync(funcArg);

        return new RuleNodeResult
        {
            Success = true,
            Response = funcArg?.RichContent?.Message?.Text ?? funcArg?.Content,
            Data = new()
            {
                ["function_name"] = canonicalName,
                ["function_argument"] = funcArg?.ConvertToString() ?? "{}",
                ["function_call_result"] = funcArg?.RichContent?.Message?.Text ?? funcArg?.Content ?? string.Empty
            }
        };
    }
}
