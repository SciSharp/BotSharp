using OpenQA.Selenium.Interactions;

namespace BotSharp.Plugin.Selenium.Drivers;

public partial class SeleniumWebDriver
{
    public async Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result)
    {
        var driver = await _instance.InitInstance(message.ContextId);
        IWebElement element = default;
        if (result.Selector.StartsWith("//"))
        {
            element = driver.FindElement(By.XPath(result.Selector));
        }
        else
        {
            element = driver.FindElement(By.CssSelector(result.Selector));
        }
        

        if (action.Action == BroswerActionEnum.Click)
        {
            if (action.Position == null)
            {
                element.Click();
            }
            else
            {
                var size = element.Size;
                var actions = new Actions(driver);
                actions.MoveToElement(element)
                    .MoveByOffset((int)action.Position.X - size.Width / 2, (int)action.Position.Y - size.Height / 2)
                    .Click()
                    .Perform();
            }
        }
        else if (action.Action == BroswerActionEnum.InputText)
        {
            element.SendKeys(action.Content);
        }
    }
}
