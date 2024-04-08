using BotSharp.Abstraction.Browsing.Enums;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result)
    {
        var page = _instance.GetPage(message.ConversationId);
        ILocator locator = page.Locator(result.Selector);

        if (action.Action == BroswerActionEnum.Click)
        {
            await locator.ClickAsync();
        }
        else if (action.Action == BroswerActionEnum.InputText)
        {
            await locator.FillAsync(action.Content);
        }
    }
}
