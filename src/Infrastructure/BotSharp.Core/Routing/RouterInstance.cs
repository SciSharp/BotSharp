using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class RouterInstance : IRouterInstance
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected readonly RoutingSettings _settings;

    private Agent _router;
    public Agent Router => _router;
    public virtual string AgentId => _router.Id;

    public RouterInstance(IServiceProvider services,
        ILogger<RouterInstance> logger,
        RoutingSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public IRouterInstance Load()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        _router = new Agent()
        {
            Id = _settings.RouterId,
            Name = _settings.RouterName,
            Description = _settings.Description
        };
        var agents = db.Agents.Where(x => !x.Disabled && x.AllowRouting).ToArray();

        // Assemble prompt
        var prompt = @$"You're {_settings.RouterName} ({_settings.Description}). Follow these steps to handle user's request:
1. Read the CONVERSATION context.
2. Select a appropriate function from FUNCTIONS.
3. Determine which agent is suitable according to conversation context.
4. Re-think about selected function is from FUNCTIONS to handle the request.
5. Make sure agent is not in args.";

        // Append function
        prompt += "\r\n";
        prompt += "\r\nFUNCTIONS";
        GetHandlers().Select((handler, i) =>
        {
            prompt += "\r\n";
            prompt += $"\r\n{i + 1}. {handler.Name}";
            prompt += $"\r\n{handler.Description}";

            // Append parameters
            if (handler.Parameters.Any())
            {
                prompt += "\r\nParameters:";
                handler.Parameters.Select((p, i) =>
                {
                    prompt += $"\r\n    - {p.Name}: {p.Description}";
                    return p;
                }).ToList();
            }

            return handler;
        }).ToList();

        prompt += "\r\n";
        prompt += "\r\nAGENTS";
        agents.Select(x => new RoutingItem
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            RequiredFields = x.RoutingRules.Where(x => x.Required)
                .Select(x => new NameDesc(x.Field, x.Description))
                .ToList()
        }).Select((agent, i) =>
        {
            prompt += "\r\n";
            prompt += $"\r\n{i + 1}. {agent.Name}";
            prompt += $"\r\n{agent.Description}";

            // Append parameters
            if (agent.RequiredFields.Any())
            {
                prompt += $"\r\nRequired:";
                agent.RequiredFields.Select((field, i) =>
                {
                    prompt += $"\r\n    - {field.Name}: {field.Description}";
                    return field;
                }).ToList();
            }
            return agent;
        }).ToList();

        prompt += "\r\n";
        prompt += "\r\nCONVERSATION";
        _router.Instruction = prompt;

        return this;
    }

    public IRouterInstance WithDialogs(List<RoleDialogModel> dialogs)
    {
        foreach (var dialog in dialogs.TakeLast(20))
        {
            _router.Instruction += $"\r\n{dialog.Role}: {dialog.Content}";
        }
        return this;
    }

    public List<RoutingHandlerDef> GetHandlers()
    {
        return _services.GetServices<IRoutingHandler>()
            .Where(x => x.IsReasoning == _settings.EnableReasoning)
            .Where(x => !string.IsNullOrEmpty(x.Description))
            .Select(x => new RoutingHandlerDef
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

        var agents = db.Agents.Where(x => !x.Disabled && x.AllowRouting).ToArray();
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
        var name = state.GetState("channel");
        var specifiedProfile = agents.FirstOrDefault(x => x.Profiles.Contains(name));
        if (specifiedProfile != null)
        {
            records = records.Where(x => specifiedProfile.Profiles.Contains(name)).ToArray();
        }

        return records;
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
