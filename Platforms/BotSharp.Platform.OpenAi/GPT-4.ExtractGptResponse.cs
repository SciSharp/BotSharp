using Abstraction.Enums;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using StackExchange.Redis;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGPT;

public partial class GPT4
{
    private async Task ExtractResponseAsync(string sessionId, LlmResponseExtractType format)
    {
        var redis = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromSeconds(60));
        var token = tokenSource.Token;
        Console.WriteLine();

        await Task.Run(async () => 
        {
            while (!token.IsCancellationRequested )
            {
                Thread.Sleep(100);
                var regenerateResponse = _driver.FindElements(By.XPath("//div[text()='Regenerate response']"));
                var end = regenerateResponse.Count > 0 ? "1" : "0";
                var element = _driver.FindElements(By.CssSelector("main div.text-base")).Last();
                var content = element.GetAttribute("innerText").Trim();

                if (string.IsNullOrEmpty(content))
                {
                    continue;
                }

                await db.StreamAddAsync(sessionId, new[] 
                {
                    new NameValueEntry("content", content),
                    new NameValueEntry("end", end)
                });

                if(end == "1")
                {
                    tokenSource.Cancel();
                }
            }
        });
    }

    private string ExtractGptResponse(LlmResponseExtractType format)
    {
        new WebDriverWait(_driver, TimeSpan.FromSeconds(180))
            .Until(drv => drv.FindElement(By.XPath("//div[text()='Regenerate response']")));

        var element = _driver.FindElements(By.CssSelector("main div.text-base")).Last();
        if (string.IsNullOrEmpty(element.GetAttribute("innerText")))
        {
            return string.Empty;
        }

        var content = element.GetAttribute("innerText");
        if (format == LlmResponseExtractType.Json)
        {
            content = content.Replace("\r", "").Replace("\n", "");
            content = content.Replace("Explanation:", "\nExplanation:");
            var json = Regex.Match(content, "{[ ]{0,}(\").*}").Value;
            return json;
        }
        else
        {
            var responses = _driver.FindElements(By.CssSelector("div.markdown"));
            if (format == LlmResponseExtractType.Html)
                return responses.Last().GetAttribute("innerHTML");
            else
                return responses.Last().Text;
        }
    }
}
