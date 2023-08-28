using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using Fluid;
using Microsoft.Extensions.Options;

namespace BotSharp.Core.Templating;

public class TemplateRender : ITemplateRender
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private static readonly FluidParser _parser = new FluidParser();
    private TemplateOptions _options;

    public TemplateRender(IServiceProvider services, ILogger<TemplateRender> logger)
    {
        _services = services;
        _logger = logger;
        _options = new TemplateOptions();
        _options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.SnakeCase;
        _options.MemberAccessStrategy.Register<RoutingRecord>();
    }

    public bool Render(Agent agent, Dictionary<string, object> dict)
    {
        var template = agent.Instruction;
        if (_parser.TryParse(template, out var t, out var error))
        {
            var context = new TemplateContext(dict, _options);
            agent.Instruction = t.Render(context);
            return true;
        }
        else
        {

            return false;
        }
    }
}
