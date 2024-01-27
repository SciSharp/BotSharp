namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    private readonly IServiceProvider _services;
    private readonly PlaywrightInstance _instance;
    public PlaywrightInstance Instance => _instance;

    public PlaywrightWebDriver(IServiceProvider services, PlaywrightInstance instance)
    {
        _services = services;
        _instance = instance;
    }

    private ILocator Locator(HtmlElementContextOut context)
    {
        ILocator element = default;
        if (!string.IsNullOrEmpty(context.ElementId))
        {
            // await _instance.Page.WaitForSelectorAsync($"#{htmlElementContextOut.ElementId}", new PageWaitForSelectorOptions { Timeout = 3 });
            element = _instance.Page.Locator($"#{context.ElementId}");
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
