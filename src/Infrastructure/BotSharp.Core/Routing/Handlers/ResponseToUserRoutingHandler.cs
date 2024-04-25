using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ResponseToUserRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "response_to_user";

    public string Description => "When you can handle the conversation without asking specific agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", 
            "why response to user directly without go to other agents."),
        new ParameterPropertyDef("response",
            "response content to user in courteous words with language English. If the user wants to end the conversation, you must set conversation_end to true and response politely."),
        new ParameterPropertyDef("conversation_end", 
            "whether to end this conversation.", 
            type: "boolean"),
        new ParameterPropertyDef("task_completed ", 
            "whether the user's task request has been completed.", 
            type: "boolean"),
        new ParameterPropertyDef("language",
            "User preferred language, considering the whole conversation. Language could be English, Spanish or Chinese.",
            required: true)
    };

    public ResponseToUserRoutingHandler(IServiceProvider services, ILogger<ResponseToUserRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var response = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            CurrentAgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            StopCompletion = true
        };

        _dialogs.Add(response);

        return true;
    }
}
