using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class RetrieveDataFromAgentRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "retrieve_data_from_agent";

    public string Description => "Retrieve data from appropriate agent.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("agent", "the name of the agent"),
        new NameDesc("question", "the question you will ask the agent to get the necessary data"),
        new NameDesc("reason", "why retrieve data"),
        new NameDesc("args", "required parameters extracted from question and hand over to the next agent")
    };

    public bool IsReasoning => true;

    public RetrieveDataFromAgentRoutingHandler(IServiceProvider services, ILogger<RetrieveDataFromAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        // Retrieve information from specific agent
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetAgents(inst.AgentName).FirstOrDefault();
        var response = await routing.InvokeAgent(record.Id);

        inst.Response = response.Content;

        /*_dialogs.Add(new RoleDialogModel(AgentRole.Assistant, inst.Parameters.Question)
        {
            CurrentAgentId = record.Id
        });*/

        _router.Instruction += $"\r\n{AgentRole.Assistant}: {inst.Question}";

        /*_dialogs.Add(new RoleDialogModel(AgentRole.Function, inst.Parameters.Answer)
        {
            FunctionName = inst.Function,
            FunctionArgs = JsonSerializer.Serialize(inst.Parameters.Arguments),
            ExecutionResult = inst.Parameters.Answer,
            ExecutionData = response.ExecutionData,
            CurrentAgentId = record.Id
        });*/

        _router.Instruction += $"\r\n{AgentRole.Function}: {response.Content}";

        // Got the response from agent, then send to reasoner again to make the decision
        // inst = await GetNextInstructionFromReasoner($"What's the next step based on user's original goal and function result?");

        return null;
    }
}
