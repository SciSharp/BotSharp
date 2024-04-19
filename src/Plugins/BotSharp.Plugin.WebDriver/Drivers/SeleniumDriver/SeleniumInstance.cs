using OpenQA.Selenium.Chrome;
using System.IO;

namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public class SeleniumInstance : IDisposable
{
    Dictionary<string, IWebDriver> _contexts = new Dictionary<string, IWebDriver>();

    public Dictionary<string, IWebDriver> Contexts => _contexts;

    public INavigation GetPage(string id)
    {
        InitInstance(id).Wait();
        return _contexts[id].Navigate();
    }

    public string GetPageContent(string id)
    {
        InitInstance(id).Wait();
        return _contexts[id].PageSource;
    }

    public async Task<IWebDriver> InitInstance(string id)
    {
        return await InitContext(id);
    }

    public async Task<IWebDriver> InitContext(string id)
    {
        if (_contexts.ContainsKey(id))
            return _contexts[id];

        string tempFolderPath = $"{Path.GetTempPath()}\\_selenium\\{id}";

        var options = new ChromeOptions();
        options.AddArgument("disable-infobars");
        options.AddArgument($"--user-data-dir={tempFolderPath}");
        var selenium = new ChromeDriver(options);
        selenium.Manage().Window.Maximize();
        selenium.Navigate().GoToUrl("about:blank");
        _contexts[id] = selenium;

        return _contexts[id];
    }

    public async Task<INavigation> NewPage(string id)
    {
        await InitContext(id);
        var selenium = _contexts[id];
        selenium.Navigate().GoToUrl("about:blank");
        return _contexts[id].Navigate();
    }

    public async Task Wait(string id)
    {
        if (_contexts.ContainsKey(id))
        {
            _contexts[id].Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }
        await Task.Delay(100);
    }

    public async Task Close(string id)
    {
        if (_contexts.ContainsKey(id))
        {
            _contexts[id].Quit();
            _contexts.Remove(id);
        }
    }

    public async Task CloseCurrentPage(string id)
    {
        if (_contexts.ContainsKey(id))
        {
        }
    }

    public void Dispose()
    {
        foreach(var context in _contexts)
        {
            context.Value.Quit();
        }
        _contexts.Clear();
    }
}
