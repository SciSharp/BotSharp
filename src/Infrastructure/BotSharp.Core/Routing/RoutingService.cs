using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
namespace BotSharp.Core.Routing;

public class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
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
        ILogger<RoutingService> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }


    public async Task<RoleDialogModel> ExecuteOnce(Agent agent)
    {
        var message = Dialogs.Last().Content;

        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");
        handler.SetDialogs(Dialogs);
        var result = await handler.Handle(new FunctionCallFromLlm
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
        var router = LoadRouter();
        var result = new RoleDialogModel(AgentRole.Assistant, "Can you repeat your request again?")
        {
            CurrentAgentId = router.Id
        };

        var message = Dialogs.Last().Content;
        foreach (var dialog in Dialogs.TakeLast(20))
        {
            router.Instruction += $"\r\n{dialog.Role}: {dialog.Content}";
        }

        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "get_next_instruction");
        handler.SetRouter(router);
        handler.SetDialogs(Dialogs);

        int loopCount = 0;
        var stop = false;
        while (!stop && loopCount < 5)
        {
            loopCount++;

            var prompt = _settings.EnableReasoning ? "Tell me the next step?" : "Which agent is suitable to handle user's request?";
            var inst = await handler.GetNextInstructionFromReasoner(prompt);
            inst.Question = inst.Question ?? message;

            handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
            if (handler == null)
            {
                handler = handlers.FirstOrDefault(x => x.Name == "get_next_instruction");
                router.Instruction += $"\r\n{AgentRole.System}: the function must be one of {string.Join(",", GetHandlers().Select(x => x.Name))}.";
                continue;
            }
            handler.SetRouter(router);
            handler.SetDialogs(Dialogs);

            result = await handler.Handle(inst);

            message = result.Content.Replace("\r\n", " ");
            router.Instruction += $"\r\n{result.Role}: {message}";

            stop = !_settings.EnableReasoning;
        }

        return result;
    }

    public Agent LoadRouter()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var router = new Agent()
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
        router.Instruction = prompt;

        return router;
    }

    private List<RoutingHandlerDef> GetHandlers()
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
}
