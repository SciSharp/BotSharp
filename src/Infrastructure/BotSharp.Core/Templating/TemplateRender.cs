using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Templating;

public class TemplateRender : ITemplateRender
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IRenderConfiguration _renderConfig;

    public TemplateRender(
        IServiceProvider services,
        ILogger<TemplateRender> logger,
        IRenderConfiguration renderConfig)
    {
        _services = services;
        _logger = logger;
        _renderConfig = renderConfig;
    }

    public string Render(string template, IDictionary<string, object> dict)
    {
        return _renderConfig.Render(_services, template, dict);
    }
}
