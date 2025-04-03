
using BotSharp.Abstraction.Browsing.Settings;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver : IWebBrowser
{
    private IServiceProvider _services => _instance.Services;
    private readonly PlaywrightInstance _instance;
    private readonly ILogger _logger;
    private readonly WebBrowsingSettings _webBrowsingSettings;
    public PlaywrightInstance Instance => _instance;

    public Agent Agent => _agent;
    private Agent _agent;

    public PlaywrightWebDriver(IServiceProvider services, PlaywrightInstance instance, ILogger<PlaywrightWebDriver> logger, WebBrowsingSettings webBrowsingSettings)
    {
        _instance = instance;
        _logger = logger;
        _instance.SetServiceProvider(services);
        _webBrowsingSettings = webBrowsingSettings;
     }

    public void SetAgent(Agent agent)
    {
        _agent = agent;
    }

    private ILocator? Locator(string contextId, HtmlElementContextOut context)
    {
        ILocator element = default;
        if (!string.IsNullOrEmpty(context.ElementId))
        {
            element = _instance.GetPage(contextId).Locator($"#{context.ElementId}");
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
            element = _instance.GetPage(contextId).Locator($"[name='{context.ElementName}']");
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
            element = _instance.GetPage(contextId).Locator(context.TagName).Nth(context.Index);
        }

        return element;
    }

    public void SetServiceProvider(IServiceProvider services)
    {
        _instance.SetServiceProvider(_services);
    }

    public async Task PressKey(MessageInfo message, string key)
    {
        var page = _instance.GetPage(message.ContextId);
        if (page != null)
        {
            await page.Keyboard.PressAsync(key);
        }
    }

    public async Task<bool> IsBrowserClosed(string contextId)
    {
        return !_instance.Contexts.ContainsKey(contextId);
    }
}
