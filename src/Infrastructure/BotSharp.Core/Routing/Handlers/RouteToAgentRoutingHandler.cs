using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class RouteToAgentRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "route_to_agent";

    public string Description => "Route request to appropriate virtual agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("next_action_reason", 
            "the reason why route to this virtual agent.", 
            required: true),
        new ParameterPropertyDef("next_action_agent", 
            "agent for next action based on user latest response, if user is replying last agent's question, you must route to this agent.", 
            required: true),
        new ParameterPropertyDef("args",
            "useful parameters of next action agent, format: { }",
            type: "object"),
        new ParameterPropertyDef("user_goal_description", 
            "user goal based on user initial task.", 
            required: true),
        new ParameterPropertyDef("user_goal_agent",
            "agent who can acheive user initial task,  must align with user_goal_description.", 
            required: true),
        new ParameterPropertyDef("conversation_end",
            "user is ending the conversation.",
            type: "boolean",
            required: true),
        new ParameterPropertyDef("is_new_task",
            "whether the user is requesting a new task that is different from the previous topic.", 
            type: "boolean")
    };

    public RouteToAgentRoutingHandler(IServiceProvider services, ILogger<RouteToAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message, Func<RoleDialogModel, Task> onFunctionExecuting)
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

        if (inst.IsNewTask)
        {
            await HookEmitter.Emit<IConversationHook>(_services, async hook =>
                await hook.OnNewTaskDetected(message, inst.NextActionReason)
            );
        }

        message.FunctionArgs = JsonSerializer.Serialize(inst);
        if (message.FunctionName != null)
        {
            var msg = RoleDialogModel.From(message, role: AgentRole.Function);
            var ret = await routing.InvokeFunction(message.FunctionName, msg);
        }

        var agentId = routing.Context.GetCurrentAgentId();

        // Update next action agent's name
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);
        inst.AgentName = agent.Name;

        if (inst.ExecutingDirectly)
        {
            message.Content = inst.Question;
        }

        if (agent.Disabled)
        {
            var content = $"This agent ({agent.Name}) is disabled, please install the corresponding plugin ({agent.Plugin.Name}) to activate this agent.";

            message = RoleDialogModel.From(message,
                role: AgentRole.Assistant,
                content: content);
            _dialogs.Add(message);
        }
        else
        {
            var ret = await routing.InvokeAgent(agentId, _dialogs, onFunctionExecuting);
        }

        var response = _dialogs.Last();
        inst.Response = response.Content;

        return true;
    }
}
