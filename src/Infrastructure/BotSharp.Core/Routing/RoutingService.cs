using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

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

        var planner = _services.GetRequiredService<IPlaner>();
        var executor = _services.GetRequiredService<IExecutor>();

        int loopCount = 0;
        var stop = false;
        while (!stop && loopCount < 5)
        {
            loopCount++;

            var conversation = await GetConversationContent(Dialogs);

            // Get instruction from Planner
            var inst = await planner.GetNextInstruction(router, conversation);

            // Fix LLM malformed response
            FixMalformedResponse(inst);

            // Save states
            SaveStateByArgs(inst.Arguments);

#if DEBUG
            Console.WriteLine($"*** Next Instruction *** {inst}", Color.GreenYellow);
#else
            _logger.LogInformation($"*** Next Instruction *** {inst}");
#endif

            // Handle instruction by Executor
            var executed = await executor.Execute(this, router, inst, Dialogs, message);

            await planner.AgentExecuted(inst, message);

            // There is no need for the agent to continue processing, indicating that the task has been completed.
            if (inst.AgentName == null)
            {
                break;
            }
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
