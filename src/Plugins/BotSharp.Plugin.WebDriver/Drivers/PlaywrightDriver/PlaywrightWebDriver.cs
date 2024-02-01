using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    private readonly IServiceProvider _services;
    private readonly PlaywrightInstance _instance;
    private readonly ILogger _logger;
    public PlaywrightInstance Instance => _instance;

    public PlaywrightWebDriver(IServiceProvider services, PlaywrightInstance instance, ILogger<PlaywrightWebDriver> logger)
    {
        _services = services;
        _instance = instance;
        _logger = logger;
    }

    private ILocator Locator(HtmlElementContextOut context)
    {
        ILocator element = default;
        if (!string.IsNullOrEmpty(context.ElementId))
        {
            // await _instance.Page.WaitForSelectorAsync($"#{htmlElementContextOut.ElementId}", new PageWaitForSelectorOptions { Timeout = 3 });
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
            // await _instance.Page.WaitForSelectorAsync($"#{htmlElementContextOut.ElementId}", new PageWaitForSelectorOptions { Timeout = 3 });
            element = _instance.Page.Locator($"[name='{context.ElementName}']");

            if (element.CountAsync().Result == 0)
            {
                _logger.LogError($"Can't locate element {role} {context.ElementName}");
            }
        }
        else
        {
            if (context.Index < 0)
            {
                throw new Exception($"Can't locate the web element {context.Index}.");
            }
            element = _instance.Page.Locator(context.TagName).Nth(context.Index);
        }

        return element;
    }
}
