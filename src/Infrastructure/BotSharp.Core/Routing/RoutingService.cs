using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public partial class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
    private readonly IRouterInstance _routerInstance;
    private readonly ILogger _logger;
    private List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs {
        get
        {
            if (_dialogs == null)
            {
                var conv = _services.GetRequiredService<IConversationService>();
                _dialogs = conv.GetDialogHistory();
            }

            return _dialogs;
        }
    }

    public RoutingService(IServiceProvider services,
        RoutingSettings settings,
        ILogger<RoutingService> logger,
         IRouterInstance routerInstance)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
        _routerInstance = routerInstance;
    }


    public async Task<RoleDialogModel> ExecuteOnce(Agent agent)
    {
        var message = Dialogs.Last().Content;

        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");
        handler.SetDialogs(Dialogs);
        var result = await handler.Handle(this, new FunctionCallFromLlm
        {
            Function = "route_to_agent",
            Question = message,
            Reason = message,
            AgentName = agent.Name
        });

        return result;
    }

    public async Task<RoleDialogModel> InstructLoop()
    {
        _routerInstance.Load().WithDialogs(Dialogs);
        var router = _routerInstance.Router;

        var result = new RoleDialogModel(AgentRole.Assistant, "Can you repeat your request again?")
        {
            CurrentAgentId = router.Id
        };

        var message = Dialogs.Last().Content;

        var handlers = _services.GetServices<IRoutingHandler>();

        int loopCount = 0;
        var stop = false;
        while (!stop && loopCount < 5)
        {
            loopCount++;

            var prompt = _settings.EnableReasoning ? "Tell me the next step?" : "Which agent is suitable to handle user's request based on the CONVERSATION?";
            prompt += " Or you can handle without asking specific agent.";
            var inst = await GetNextInstruction(prompt);
            inst.Question = inst.Question ?? message;

            var handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
            if (handler == null)
            {
                handler = handlers.FirstOrDefault(x => x.Name == "get_next_instruction");
                router.Instruction += $"\r\n{AgentRole.System}: the function must be one of {string.Join(",", _routerInstance.GetHandlers().Select(x => x.Name))}.";
                continue;
            }
            handler.SetRouter(router);
            handler.SetDialogs(Dialogs);

            result = await handler.Handle(this, inst);

            message = result.Content.Replace("\r\n", " ");
            router.Instruction += $"\r\n{result.Role}: {message}";

            stop = !_settings.EnableReasoning;
        }

        return result;
    }

    protected void SaveStateByArgs(JsonDocument args)
    {
        if (args == null)
        {
            return;
        }

        var stateService = _services.GetRequiredService<IConversationStateService>();
        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    stateService.SetState(property.Name, property.Value);
                }
            }
        }
    }
}
