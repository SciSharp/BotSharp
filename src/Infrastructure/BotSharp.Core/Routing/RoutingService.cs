using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

namespace BotSharp.Core.Routing;

public partial class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
    private readonly ILogger _logger;
    private Agent _router;
    public Agent Router => _router;

    public void ResetRecursiveCounter()
    {
        _currentRecursionDepth = 0;
    }

    public RoutingService(IServiceProvider services,
        RoutingSettings settings,
        ILogger<RoutingService> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public async Task<RoleDialogModel> ExecuteDirectly(Agent agent, RoleDialogModel message)
    {
        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");

        var conv = _services.GetRequiredService<IConversationService>();
        var dialogs = conv.GetDialogHistory();
        handler.SetDialogs(dialogs);

        var inst = new FunctionCallFromLlm
        {
            Function = "route_to_agent",
            Question = message.Content,
            Reason = message.Content,
            AgentName = agent.Name,
            OriginalAgent = agent.Name,
            ExecutingDirectly = true
        };

        var result = await handler.Handle(this, inst, message);

        var response = dialogs.Last();
        response.MessageId = message.MessageId;
        response.Instruction = inst;

        return response;
    }

    public async Task<RoleDialogModel> InstructLoop(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        _router = await agentService.LoadAgent(message.CurrentAgentId);

        RoleDialogModel response = default;

        var states = _services.GetRequiredService<IConversationStateService>();
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
            _router.TemplateDict["planner"] = _settings.Planner;

            // Get instruction from Planner
            var inst = await planner.GetNextInstruction(_router, message.MessageId);

            // Save states
            states.SaveStateByArgs(inst.Arguments);

#if DEBUG
            Console.WriteLine($"*** Next Instruction *** {inst}", Color.GreenYellow);
#else
            _logger.LogInformation($"*** Next Instruction *** {inst}");
#endif
            await planner.AgentExecuting(_router, inst, message);

            // Handle instruction by Executor
            response = await executor.Execute(this, inst, message, dialogs);

            await planner.AgentExecuted(_router, inst, response);
        }

        return response;
    }

    public List<RoutingHandlerDef> GetHandlers()
    {
        var planer = _services.GetRequiredService<IPlaner>();

        return _services.GetServices<IRoutingHandler>()
            .Where(x => x.Planers == null || x.Planers.Contains(planer.GetType().Name))
            .Where(x => !string.IsNullOrEmpty(x.Description))
            .Select((x, i) => new RoutingHandlerDef
            {
                Name = x.Name,
                Description = x.Description,
                Parameters = x.Parameters
            }).ToList();
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    protected RoutingRule[] GetRoutingRecords()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var filter = new AgentFilter
        {
            Disabled = false,
            AllowRouting = true
        };
        var agents = db.GetAgents(filter);
        var records = agents.SelectMany(x =>
        {
            x.RoutingRules.ForEach(r =>
            {
                r.AgentId = x.Id;
                r.AgentName = x.Name;
            });
            return x.RoutingRules;
        }).ToArray();

        // Filter agents by profile
        var state = _services.GetRequiredService<IConversationStateService>();
        var channel = state.GetState("channel");
        var specifiedProfile = agents.FirstOrDefault(x => x.Profiles.Contains(channel));
        if (specifiedProfile != null)
        {
            records = records.Where(x => specifiedProfile.Profiles.Contains(channel)).ToArray();
        }

        return records;
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public RoutingItem[] GetRoutingItems()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var filter = new AgentFilter
        {
            Disabled = false,
            AllowRouting = true
        };
        var agents = db.GetAgents(filter);
        return agents.Select(x => new RoutingItem
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            RequiredFields = x.RoutingRules
                .Where(p => p.Required)
                .Select(p => new ParameterPropertyDef(p.Field, p.Description, type: p.Type)
                {
                    Required = p.Required
                }).ToList(),
            OptionalFields = x.RoutingRules
                .Where(p => !p.Required)
                .Select(p => new ParameterPropertyDef(p.Field, p.Description, type: p.Type)
                {
                    Required = p.Required
                }).ToList()
        }).ToArray();
    }

    public RoutingRule[] GetRulesByName(string name)
    {
        return GetRoutingRecords()
            .Where(x => x.AgentName.ToLower() == name.ToLower())
            .ToArray();
    }

    public RoutingRule[] GetRulesByAgentId(string id)
    {
        return GetRoutingRecords()
            .Where(x => x.AgentId == id)
            .ToArray();
    }
}
