using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

namespace BotSharp.Core.Routing;

public partial class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
    private readonly IRoutingContext _context;
    private readonly ILogger _logger;
    private Agent _router;

    public IRoutingContext Context => _context;
    public Agent Router => _router;

    public void ResetRecursiveCounter()
    {
        _currentRecursionDepth = 0;
    }

    public RoutingService(IServiceProvider services,
        RoutingSettings settings,
        IRoutingContext context,
        ILogger<RoutingService> logger)
    {
        _services = services;
        _settings = settings;
        _context = context;
        _logger = logger;
    }

    public async Task<RoleDialogModel> InstructDirect(Agent agent, RoleDialogModel message)
    {
        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");

        var conv = _services.GetRequiredService<IConversationService>();
        var storage = _services.GetRequiredService<IConversationStorage>();
        storage.Append(conv.ConversationId, message);

        var dialogs = conv.GetDialogHistory();
        handler.SetDialogs(dialogs);

        var inst = new FunctionCallFromLlm
        {
            Function = "route_to_agent",
            Question = message.Content,
            NextActionReason = message.Content,
            AgentName = agent.Name,
            OriginalAgent = agent.Name,
            ExecutingDirectly = true
        };

        var result = await handler.Handle(this, inst, message, null);

        var response = dialogs.Last();
        response.MessageId = message.MessageId;
        response.Instruction = inst;

        return response;
    }

    public async Task<RoleDialogModel> InstructLoop(RoleDialogModel message, List<RoleDialogModel> dialogs, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        RoleDialogModel response = default;

        var agentService = _services.GetRequiredService<IAgentService>();
        var convService = _services.GetRequiredService<IConversationService>();
        var storage = _services.GetRequiredService<IConversationStorage>();

        _router = await agentService.LoadAgent(message.CurrentAgentId);

        var states = _services.GetRequiredService<IConversationStateService>();
        var executor = _services.GetRequiredService<IExecutor>();

        var planner = GetPlanner(_router);

        _context.Push(_router.Id);

        // Handle multi-language for input
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        if (agentSettings.EnableTranslator)
        {
            var translator = _services.GetRequiredService<ITranslationService>();

            var language = states.GetState(StateConst.LANGUAGE, LanguageType.ENGLISH);
            if (language != LanguageType.ENGLISH)
            {
                message.SecondaryContent = message.Content;
                message.Content = await translator.Translate(_router, message.MessageId, message.Content,
                    language: LanguageType.ENGLISH,
                    clone: false);
            }
        }

        dialogs.Add(message);
        storage.Append(convService.ConversationId, message);

        // Get first instruction
        _router.TemplateDict["conversation"] = await GetConversationContent(dialogs);
        var inst = await planner.GetNextInstruction(_router, message.MessageId, dialogs);

        int loopCount = 1;
        while (true)
        {
            await HookEmitter.Emit<IRoutingHook>(_services, async hook =>
                await hook.OnRoutingInstructionReceived(inst, message)
            );

            // Save states
            states.SaveStateByArgs(inst.Arguments);

#if DEBUG
            Console.WriteLine($"*** Next Instruction *** {inst}");
#else
            _logger.LogInformation($"*** Next Instruction *** {inst}");
#endif
            await planner.AgentExecuting(_router, inst, message, dialogs);

            // Handover to Task Agent
            if (inst.HandleDialogsByPlanner)
            {
                var dialogWithoutContext = planner.BeforeHandleContext(inst, message, dialogs);
                response = await executor.Execute(this, inst, message, dialogWithoutContext, onFunctionExecuting);
                planner.AfterHandleContext(dialogs, dialogWithoutContext);
            }
            else
            {
                response = await executor.Execute(this, inst, message, dialogs, onFunctionExecuting);
            }

            await planner.AgentExecuted(_router, inst, response, dialogs);

            if (loopCount >= planner.MaxLoopCount || _context.IsEmpty)
            {
                break;
            }

            // Get next instruction from Planner
            _router.TemplateDict["conversation"] = await GetConversationContent(dialogs);
            inst = await planner.GetNextInstruction(_router, message.MessageId, dialogs);
            loopCount++;
        }

        return response;
    }

    public List<RoutingHandlerDef> GetHandlers(Agent router)
    {
        var planer = GetPlanner(router);

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
            Type = AgentType.Task
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

        return records;
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public RoutableAgent[] GetRoutableAgents(List<string> profiles)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var filter = new AgentFilter
        {
            Disabled = false,
            Type = AgentType.Task
        };

        var agents = db.GetAgents(filter);
        var routableAgents = agents.Select(x => new RoutableAgent
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            Profiles = x.Profiles,
            RequiredFields = x.RoutingRules
                .Where(p => p.Required)
                .Select(p => new ParameterPropertyDef(p.Field, p.Description, type: p.FieldType)
                {
                    Required = p.Required
                }).ToList(),
            OptionalFields = x.RoutingRules
                .Where(p => !p.Required)
                .Select(p => new ParameterPropertyDef(p.Field, p.Description, type: p.FieldType)
                {
                    Required = p.Required
                }).ToList()
        }).ToArray();

        // Handle profile.
        // Router profile must match the agent profile
        if (routableAgents.Length > 0 && profiles.Count > 0)
        {
            routableAgents = routableAgents.Where(x => x.Profiles != null &&
                    x.Profiles.Exists(x1 => profiles.Exists(y => x1 == y)))
                .ToArray();
        }
        else if (profiles == null || profiles.Count == 0)
        {
            routableAgents = routableAgents.Where(x => x.Profiles == null ||
                x.Profiles.Count == 0)
            .ToArray();
        }

        return routableAgents;
    }

    public RoutingRule[] GetRulesByAgentName(string name)
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
