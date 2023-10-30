using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

namespace BotSharp.Core.Routing;

public partial class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
    private readonly IRouterInstance _routerInstance;
    private readonly ILogger _logger;
    private Agent _router;
    public Agent Router => _router;

    public void ResetRecursiveCounter()
    {
        _currentRecursionDepth = 0;
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

    public async Task<RoleDialogModel> ExecuteOnce(Agent agent, RoleDialogModel message)
    {
        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");
        var dialogs = new List<RoleDialogModel> { message };
        handler.SetDialogs(dialogs);

        var inst = new FunctionCallFromLlm
        {
            Function = "route_to_agent",
            Question = message.Content,
            Reason = message.Content,
            AgentName = agent.Name
        };

        var result = await handler.Handle(this, inst, message);

        var response = dialogs.Last();
        response.MessageId = message.MessageId;
        response.Instruction = inst;

        return response;
    }

    public async Task<RoleDialogModel> InstructLoop(RoleDialogModel message)
    {
        _router = _routerInstance.Load()
            .Router;

        RoleDialogModel response = default;

        var conv = _services.GetRequiredService<IConversationService>();
        var dialogs = conv.GetDialogHistory();

        var context = _services.GetRequiredService<RoutingContext>();
        var planner = _services.GetRequiredService<IPlaner>();
        var executor = _services.GetRequiredService<IExecutor>();

        context.Push(_router.Id);

        int loopCount = 0;
        while (loopCount < 5 && !context.IsEmpty)
        {
            loopCount++;

            var conversation = await GetConversationContent(dialogs);
            _router.TemplateDict["conversation"] = conversation;

            // Get instruction from Planner
            var inst = await planner.GetNextInstruction(_router, message.MessageId);

            // Save states
            SaveStateByArgs(inst.Arguments);

#if DEBUG
            Console.WriteLine($"*** Next Instruction *** {inst}", Color.GreenYellow);
#else
            _logger.LogInformation($"*** Next Instruction *** {inst}");
#endif
            await planner.AgentExecuting(inst, message);

            // Handle instruction by Executor
            response = await executor.Execute(this, inst, message, dialogs);

            await planner.AgentExecuted(inst, response);
        }

        return response;
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
