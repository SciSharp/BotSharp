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

    public void ResetRecursiveCounter()
    {
        _currentRecursionDepth = 0;
    }

    public void RefreshDialogs()
    {
        _dialogs = null;
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

    public async Task<bool> ExecuteOnce(Agent agent, RoleDialogModel message)
    {
        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");
        handler.SetDialogs(Dialogs);

        var result = await handler.Handle(this, new FunctionCallFromLlm
        {
            Function = "route_to_agent",
            Question = message.Content,
            Reason = message.Content,
            AgentName = agent.Name
        }, message);

        return result;
    }

    public async Task<bool> InstructLoop(RoleDialogModel message)
    {
        _routerInstance.Load();
        var router = _routerInstance.Router;

        var handlers = _services.GetServices<IRoutingHandler>();

        int loopCount = 0;
        var stop = false;
        while (!stop && loopCount < 5)
        {
            loopCount++;

            var inst = await GetNextInstruction();
            message.Instruction = inst;
            inst.Question = message.Content;

            var handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
            if (handler == null)
            {
                handler = handlers.FirstOrDefault(x => x.Name == "get_next_instruction");
                continue;
            }
            handler.SetRouter(router);
            handler.SetDialogs(Dialogs);

            message.FunctionName = inst.Function;
            message.Role = AgentRole.Function;
            message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);
            
            await handler.Handle(this, inst, message);

            inst.Response = message.Content;

            stop = !_settings.EnableReasoning;
        }

        return true;
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
