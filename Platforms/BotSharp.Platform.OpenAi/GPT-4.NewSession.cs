using Abstraction.Models;
using ChatGPT.Models;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using System;
using System.Linq;
using System.Threading;

namespace ChatGPT;

public partial class GPT4
{
    public LlmResponse NewSession(LlmPromptInput prompt)
    {
        if (!LoggedIn)
        {
            return new LlmResponse
            {
                Format = prompt.Format,
                Content = "Please login agent first.",
                Model = prompt.Model
            };
        }

        // Active default window
        var window = _driver.WindowHandles[0];
        _driver.SwitchTo().Window(window);

        // New session
        var newChat = _driver.FindElement(By.XPath("//a[text()='New chat']"));
        newChat.Click();

        Thread.Sleep(200);

        var models = _driver.FindElements(By.XPath($"//span[text()='{prompt.Model}']/../.."));
        if (models.Count == 0)
        {
            // Choose model
            var modelSelector = _driver.FindElement(By.XPath($"//span[text()='Default (GPT-3.5)']/../.."));
            modelSelector.Click();

            // Switch model
            var gpt4 = _driver.FindElement(By.XPath($"//span[text()='{prompt.Model}']/../.."));
            gpt4.Click();
        }

        var response = Interact(prompt, false);

        #region Get Session Id
        if (_token == null)
        {
            // Extract current session id
            if (Guid.TryParse(_driver.Url.Split('/').Last(), out Guid sessionId))
            {
                response.SessionId = sessionId.ToString();
            }
        }
        else
        {
            var js = @$"var xhr = new XMLHttpRequest();
            xhr.open('GET', 'https://chat.openai.com/backend-api/conversations?offset=0&limit=20', false);
            xhr.setRequestHeader('Content-type', 'application/json');
            xhr.setRequestHeader('authorization','Bearer {_token.AccessToken}');
            xhr.send();
            return xhr.response;";

            var json = _driver.ExecuteJavaScript<string>(js);
            var conversations = JsonConvert.DeserializeObject<ListResponse<ConversationListResponse>>(json);
            response.SessionId = conversations.Items.First().Id;
        }
        /*while (string.IsNullOrEmpty(response.Sessionid) || response.Sessionid == Guid.Empty.ToString())
        {
            _driver.WaitMiliseconds(200);

            newChat = _driver.FindElement(By.XPath("//a[text()='New chat']"));
            newChat.Click();

            _driver.WaitMiliseconds(200);

            var currentChatTabs = _driver.FindElements(By.CssSelector("nav div a"));
            currentChatTabs.First().Click();

            _driver.WaitMiliseconds(200);

            // Extract current session id
            if (Guid.TryParse(_driver.Url.Split('/').Last(), out Guid sessionId))
            {
                response.Sessionid = sessionId.ToString();
            }
        }*/
        #endregion

        newChat = _driver.FindElement(By.XPath("//a[text()='New chat']"));
        newChat.Click();

        // Open a new tab
        _driver.SwitchTo().NewWindow(WindowType.Tab);
        _driver.Navigate().GoToUrl($"https://chat.openai.com/chat/{response.SessionId}");

        _activeTabs[response.SessionId] = _driver.CurrentWindowHandle;

        return response;
    }
}
