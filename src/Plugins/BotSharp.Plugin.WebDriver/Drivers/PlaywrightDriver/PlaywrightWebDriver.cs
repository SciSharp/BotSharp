using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver : IWebBrowser
{
    private readonly IServiceProvider _services;
    private readonly PlaywrightInstance _instance;
    private readonly ILogger _logger;
    public PlaywrightInstance Instance => _instance;

    public Agent Agent => _agent;
    private Agent _agent;

    public PlaywrightWebDriver(IServiceProvider services, PlaywrightInstance instance, ILogger<PlaywrightWebDriver> logger)
    {
        _services = services;
        _instance = instance;
        _logger = logger;
    }

    public void SetAgent(Agent agent)
    {
        _agent = agent;
    }

    private ILocator? Locator(HtmlElementContextOut context)
    {
        ILocator element = default;
        if (!string.IsNullOrEmpty(context.ElementId))
        {
            element = _instance.Page.Locator($"#{context.ElementId}");
        }
        else if (!string.IsNullOrEmpty(context.ElementName))
        {
            var role = context.TagName switch
            {
                "input" => AriaRole.Textbox,
                "textarea" => AriaRole.Textbox,
                "button" => AriaRole.Button,
                _ => AriaRole.Generic
            };
            element = _instance.Page.Locator($"[name='{context.ElementName}']");
            var count = element.CountAsync().Result;
            if (count == 0)
            {
                _logger.LogError($"Can't locate element {role} {context.ElementName}");
                return null;
            }
            else if (count > 1)
            {
                _logger.LogError($"Located multiple elements {role} {context.ElementName}");
                return null;
            }
        }
        else
        {
            if (context.Index < 0)
            {
                _logger.LogError($"Can't locate the web element {context.Index}.");
                return null;
            }
            element = _instance.Page.Locator(context.TagName).Nth(context.Index);
        }

        return element;
    }
}
