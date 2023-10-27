using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ContinueExecuteTaskRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "continue_execute_task";

    public string Description => "Continue to execute user's request without further information retrival.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("agent", "the name of the agent"),
        new NameDesc("args", "required parameters extracted from question"),
        new NameDesc("reason", "why continue to execute current task")
    };

    public bool IsReasoning => true;

    public ContinueExecuteTaskRoutingHandler(IServiceProvider services, ILogger<ContinueExecuteTaskRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetAgents(inst.AgentName).FirstOrDefault();

        var result = new RoleDialogModel(AgentRole.Function, inst.Question)
        {
            MessageId = inst.MessageId,
            FunctionName = inst.Function,
            FunctionArgs = JsonSerializer.Serialize(inst.Arguments),
            CurrentAgentId = record.Id
        };

        return result;
    }
}
