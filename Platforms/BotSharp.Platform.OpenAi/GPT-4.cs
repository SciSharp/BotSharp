using Abstraction;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumUndetectedChromeDriver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChatGPT;

public partial class GPT4 : ILlmAgent
{
    private IWebDriver _driver;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// SessionId - WindowHandle
    /// </summary>
    private Dictionary<string, string> _activeTabs = new Dictionary<string, string>();

    public GPT4(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OpenDriver()
    {
        var path = await new ChromeDriverInstaller().Auto();
        var options = new ChromeOptions
        {
        };
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        _driver = UndetectedChromeDriver.Create(options,
            driverExecutablePath: path);
    }

    public void Dispose()
    {
        _driver.Quit();
    }
}
