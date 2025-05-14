using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Planning;

namespace BotSharp.Core.Routing.Reasoning;

public class InstructExecutor : IExecutor
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public InstructExecutor(IServiceProvider services, ILogger<InstructExecutor> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> Execute(IRoutingService routing,
        FunctionCallFromLlm inst,
        RoleDialogModel message,
        List<RoleDialogModel> dialogs)
    {
        var states = _services.GetRequiredService<IConversationStateService>();
        var goalAgent = states.GetState(StateConst.EXPECTED_GOAL_AGENT);
        if (!string.IsNullOrEmpty(goalAgent) && inst.OriginalAgent != goalAgent)
        {
            inst.OriginalAgent = goalAgent;
            // Emit hook
            await HookEmitter.Emit<IRoutingHook>(_services, async hook =>
                await hook.OnRoutingInstructionRevised(inst, message)
            );
        }

        message.FunctionArgs = JsonSerializer.Serialize(inst);
        if (message.FunctionName != null)
        {
            var msg = RoleDialogModel.From(message, role: AgentRole.Function);
            await routing.InvokeFunction(message.FunctionName, msg);
        }

        var agentId = routing.Context.GetCurrentAgentId();

        // Update next action agent's name
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);
        inst.AgentName = agent.Name;

        if (inst.ExecutingDirectly)
        {
            message.Content = inst.Question;
        }

        if (agent.Disabled)
        {
            var content = $"This agent ({agent.Name}) is disabled, please install the corresponding plugin ({agent.Plugin.Name}) to activate this agent.";

            message = RoleDialogModel.From(message, role: AgentRole.Assistant, content: content);
            dialogs.Add(message);
        }
        else
        {
            var ret = await routing.InvokeAgent(agentId, dialogs);
        }

        var response = dialogs.Last();
        
        response.Instruction = inst;

        return response;
    }
}
