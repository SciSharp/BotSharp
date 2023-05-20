using Abstraction.Models;
using ChatGPT.Models;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using System;
using System.Threading;

namespace ChatGPT;

public partial class GPT4
{
    public LlmResponse Interact(LlmPromptInput input, bool isAsync)
    {
        /*var msg = new ConversationPost
        {
            Action = "next",
            Model = "gpt-4",
            VariantPurpose = "none",
            Messages = new System.Collections.Generic.List<ConversationMessageBody>
            {
                new ConversationMessageBody
                {
                    Id = Guid.NewGuid().ToString(),
                    Author = new ConversationAuthor
                    {
                        Role = "user"
                    },
                    Content = new ConversationContent
                    {
                        ContentType = "text",
                        Parts = input.Prompts
                    }
                }
            },
            ParentMessageId = Guid.NewGuid().ToString()
        };

        var js = @$"var xhr = new XMLHttpRequest();
            xhr.open('POST', 'https://chat.openai.com/backend-api/conversation', false);
            xhr.setRequestHeader('Content-type', 'application/json');
            xhr.setRequestHeader('authorization','Bearer {_token.AccessToken}');
            xhr.send('{JsonConvert.SerializeObject(msg)}');
            return xhr.response;";

        var json = _driver.ExecuteJavaScript<string>(js);*/

        Console.WriteLine($"---------- {input.SessionId} {DateTime.UtcNow}");
        Console.WriteLine(string.Join('\r', input.Prompts));

        var response = new LlmResponse
        {
            SessionId = input.SessionId,
            Format = input.Format,
            Success = true
        };

        // Switch to session window
        if (!string.IsNullOrEmpty(input.SessionId) && input.SessionId != Guid.Empty.ToString())
        {
            if (!_activeTabs.ContainsKey(input.SessionId))
            {
                bool activated = false;
                foreach (var tab in _driver.WindowHandles)
                {
                    _driver.SwitchTo().Window(tab);
                    if (_driver.Url.Contains(input.SessionId))
                    {
                        activated = true;
                        break;
                    }
                }

                if (!activated)
                {
                    _driver.Navigate().GoToUrl($"https://chat.openai.com/chat/{input.SessionId}");
                }
            }
        }
        else
        {
            _driver.SwitchTo().Window(_driver.WindowHandles[0]);
        }

        Thread.Sleep(1000);
        var textarea = _driver.FindElement(By.CssSelector("main textarea"));
        bool isFirstLine = true;
        foreach (var p in input.Prompts)
        {
            if (string.IsNullOrEmpty(p))
            {
                InputNewLine();
            }
            else
            {
                if (!isFirstLine)
                {
                    InputNewLine();
                }

                var text = p;//.Replace("\r", "\n");
                /*foreach (var c in text.Split('\n'))
                {
                    if (c.Length > 128)
                    {
                        InputText(textarea, c);
                    }
                    else
                    {
                        textarea.SendKeys(c);
                    }
                    InputNewLine();
                }*/

                if (text.Length > 64)
                {
                    InputText(textarea, text);
                }
                else
                {
                    textarea.SendKeys(text);
                }
                isFirstLine = false;
            }
        }
        textarea.SendKeys(Keys.Enter);

        if (isAsync)
        {
            ExtractResponseAsync(input.SessionId, input.Format);
        }
        else
        {
            response.Content = ExtractGptResponse(input.Format);
        }

        return response;
    }
}
