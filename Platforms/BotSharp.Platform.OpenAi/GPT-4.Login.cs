using Abstraction.Models;
using ChatGPT.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGPT;

public partial class GPT4
{
    bool _isLoggedIn = false;
    public bool LoggedIn => _isLoggedIn;
    GetTokenResponse _token;

    public async Task Login()
    {
        if (_isLoggedIn)
        {
            return;
        }

        await OpenDriver();

        _driver.Navigate().GoToUrl("https://chat.openai.com/");

        var textarea = _driver.FindElements(By.CssSelector("main textarea"));
        if (textarea.Count() == 1) 
        {
            _isLoggedIn = true;
            return;
        }

        // https://selenium-python.readthedocs.io/waits.html
        new WebDriverWait(_driver, TimeSpan.FromSeconds(5))
            .Until(drv => drv.FindElement(By.TagName("button")));

        // https://selenium-python.readthedocs.io/locating-elements.html#locating-elements-by-class-name
        var loginButton = _driver.FindElement(By.TagName("button"));
        loginButton.Click();

        var openAiSettings = _serviceProvider.GetRequiredService<OpenAiSettings>();

        // login in Google account
        // LoginInGoogleAccount(openAiSettings);

        // login in username
        LoginInUserName(openAiSettings);
    }

    private void LoginInGoogleAccount(OpenAiSettings openAiSettings)
    {
        var continueWithGoogle = _driver.FindElement(By.XPath("//button[@data-provider='google']"));
        continueWithGoogle.Click();

        var email = _driver.FindElement(By.Id("identifierId"));
        email.SendKeys(openAiSettings.UserName);

        var next = _driver.FindElement(By.XPath("//span[text()='Next']/.."));
        next.Click();
    }

    private void LoginInUserName(OpenAiSettings openAiSettings)
    {
        new WebDriverWait(_driver, TimeSpan.FromSeconds(5))
            .Until(drv => drv.FindElement(By.Id("username")));
        var userName = _driver.FindElement(By.Id("username"));
        userName.SendKeys(openAiSettings.UserName);

        var actionButton = _driver.FindElement(By.Name("action"));
        actionButton.Click();

        Thread.Sleep(300);
        new WebDriverWait(_driver, TimeSpan.FromSeconds(5))
            .Until(drv => drv.FindElement(By.Id("password")));
        var password = _driver.FindElement(By.Id("password"));
        password.SendKeys(openAiSettings.Password);

        actionButton = _driver.FindElement(By.Name("action"));
        actionButton.Click();

        Thread.Sleep(300);
        /*new WebDriverWait(_driver, TimeSpan.FromSeconds(5))
            .Until(drv => drv.FindElement(By.CssSelector(@"div#headlessui-dialog-panel-\:r1\: button")));

        // https://developer.mozilla.org/en-US/docs/Learn/CSS/Building_blocks/Selectors/Type_Class_and_ID_Selectors#id_selectors
        for (int i = 0; i < 3; i++)
        {
            Thread.Sleep(300);

            var inputs = _driver.FindElements(By.CssSelector(@"div#headlessui-dialog-panel-\:r1\: button"));
            inputs.Last().Click();
        }*/

        var js = @"var xhr = new XMLHttpRequest();
            xhr.open('GET', 'https://chat.openai.com/api/auth/session', false);
            xhr.setRequestHeader('Content-type', 'application/json');
            xhr.send();
            return xhr.response;";

        var json = _driver.ExecuteJavaScript<string>(js);
        _token = JsonConvert.DeserializeObject<GetTokenResponse>(json);

        _isLoggedIn = true;
    }
}
