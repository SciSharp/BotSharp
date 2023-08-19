using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Core.Functions;

public class GoToRouterFn : IFunctionCallback
{
    public string Name => "go_to_router";
    private readonly IServiceProvider _services;

    public GoToRouterFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var settings = _services.GetRequiredService<AgentSettings>();
        message.CurrentAgentId = settings.RouterId;

        var result = new FunctionExecutionValidationResult("true");
        message.ExecutionResult = JsonSerializer.Serialize(result);

        return true;
    }
}
