using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

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

    public RoutingService(
        IServiceProvider services,
        RoutingSettings settings,
        IRoutingContext context,
        ILogger<RoutingService> logger)
    {
        _services = services;
        _settings = settings;
        _context = context;
        _logger = logger;
    }

    public async Task<RoleDialogModel> InstructDirect(Agent agent, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var storage = _services.GetRequiredService<IConversationStorage>();
        storage.Append(conv.ConversationId, message);

        dialogs.Add(message);
        Context.SetDialogs(dialogs);

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.Push(agent.Id, "instruct directly");
        var agentId = routing.Context.GetCurrentAgentId();

        // Update next action agent's name
        var agentService = _services.GetRequiredService<IAgentService>();

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
        response.MessageId = message.MessageId;

        return response;
    }

#if !DEBUG
    [SharpCache(10)]
#endif
    protected RoutingRule[] GetRoutingRecords()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var filter = new AgentFilter
        {
            Disabled = false
        };
        var agents = db.GetAgents(filter);
        var records = agents.Where(x => x.Type == AgentType.Task || x.Type == AgentType.Planning).SelectMany(x =>
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
    [SharpCache(10)]
#endif
    public RoutableAgent[] GetRoutableAgents(List<string> profiles)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var filter = new AgentFilter
        {
            Disabled = false
        };

        var agents = db.GetAgents(filter);
        var routableAgents = agents.Where(x => x.Type == AgentType.Task || x.Type == AgentType.Planning).Select(x => new RoutableAgent
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            Type = x.Type,
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
