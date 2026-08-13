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

        // 缺失/空白的 function_name 必须优雅失败——旧的 IsEqualTo 查找对 null 安全，只是匹配不到
        // 任何 callback；一旦改走工厂，null 就会传到 IFunctionExecutorProvider.TryResolve 这个
        // 按契约声明为非空 string 的形参上，某些实现（例如按名字做 Dictionary 查找的 mock/阻断
        // provider）会直接抛 ArgumentNullException，而不是像原来一样返回 Success = false。
        // 因此在碰工厂之前先挡掉。
        string? canonicalName = null;
        IFunctionExecutor? executor = null;
        if (!string.IsNullOrWhiteSpace(funcName))
        {
            // 只用注册的 callback 求"规范名"，保留原有的大小写不敏感语义；
            // 真正的执行必须走工厂，否则 IFunctionExecutorProvider 在规则路径上被旁路。
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
