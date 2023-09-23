using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
    private readonly ILogger _logger;
    private List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    public RoutingService(IServiceProvider services,
        RoutingSettings settings,
        ILogger<RoutingService> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public void SetDialogs(List<RoleDialogModel> dialogs)
    {
        _dialogs = dialogs;
    }

    public async Task<RoleDialogModel> ExecuteOnce(Agent agent)
    {
        var message = _dialogs.Last().Content;

        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "route_to_agent");
        handler.SetDialogs(_dialogs);
        var result = await handler.Handle(new FunctionCallFromLlm
        {
           Function = "route_to_agent",
           Question = message,
           Route = new RoutingArgs
           {
               Reason = message,
               AgentName = agent.Name,
           }
        });

        return result;
    }

    public async Task<RoleDialogModel> InstructLoop(Agent router)
    {
        var result = new RoleDialogModel(AgentRole.Assistant, "Can you repeat your request again?")
        {
            CurrentAgentId = router.Id
        };

        var message = _dialogs.Last().Content;
        foreach (var dialog in _dialogs.TakeLast(20))
        {
            router.Instruction += $"\r\n{dialog.Role}: {dialog.Content}";
        }

        var handlers = _services.GetServices<IRoutingHandler>();

        var handler = handlers.FirstOrDefault(x => x.Name == "get_next_instruction");
        handler.SetRouter(router);
        handler.SetDialogs(_dialogs);

        int loopCount = 0;
        var stop = false;
        while (!stop && loopCount < 5)
        {
            loopCount++;

            var inst = await handler.GetNextInstructionFromReasoner($"You are the Router, tell me the next step?");
            inst.Question = inst.Question ?? message;

            handler = handlers.FirstOrDefault(x => x.Name == inst.Function);
            if (handler == null)
            {
                handler = handlers.FirstOrDefault(x => x.Name == "get_next_instruction");
                router.Instruction += $"\r\n{AgentRole.System}: the function must be one of {string.Join(",", GetHandlers().Select(x => x.Name))}.";
                continue;
            }
            handler.SetRouter(router);
            handler.SetDialogs(_dialogs);

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

        var dict = new Dictionary<string, object>();
        dict["routing_records"] = agents.Select(x => new RoutingItem
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            RequiredFields = x.RoutingRules.Where(x => x.Required)
                .Select(x => x.Field)
                .ToArray()
        }).ToArray();

        dict["routing_handlers"] = GetHandlers();

        var render = _services.GetRequiredService<ITemplateRender>();
        router.Instruction = render.Render(PromptConst.ROUTER_PROMPT, dict);

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
