namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver : IWebBrowser
{
    private readonly IServiceProvider _services;
    private readonly SeleniumInstance _instance;
    private readonly ILogger _logger;
    public SeleniumInstance Instance => _instance;

    public Agent Agent => _agent;
    private Agent _agent;

    public SeleniumWebDriver(IServiceProvider services, SeleniumInstance instance, ILogger<SeleniumWebDriver> logger)
    {
        _services = services;
        _instance = instance;
        _logger = logger;
    }

    public Task<BrowserActionResult> ActionOnElement(MessageInfo message, ElementLocatingArgs location, ElementActionArgs action)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> ChangeCheckbox(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> ChangeListValue(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> CheckRadioButton(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> ClickButton(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> ClickElement(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task CloseBrowser(string contextId)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> CloseCurrentPage(MessageInfo message)
    {
        throw new NotImplementedException();
    }

    public Task<string> ExtractData(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> InputUserPassword(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> InputUserText(BrowserActionParams actionParams)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> ScreenshotAsync(MessageInfo message, string path)
    {
        throw new NotImplementedException();
    }

    public Task<BrowserActionResult> ScrollPage(MessageInfo message, PageActionArgs args)
    {
        throw new NotImplementedException();
    }
}
