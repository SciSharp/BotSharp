namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result)
    {
        var page = _instance.GetPage(message.ContextId);
        ILocator locator = page.Locator(result.Selector);

        if (action.Action == BroswerActionEnum.Click)
        {
            if (action.Position == null)
            {
                await locator.ClickAsync();
            }
            else
            {
                await locator.ClickAsync(new LocatorClickOptions
                {
                    Position = new Position
                    {
                        X = action.Position.X,
                        Y = action.Position.Y
                    }
                });
            }
        }
        else if (action.Action == BroswerActionEnum.InputText)
        {
            await locator.FillAsync(action.Content);

            if (action.PressKey != null)
            {
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Typing)
        {
            await locator.PressSequentiallyAsync(action.Content);
            if (action.PressKey != null)
            {
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Hover)
        {
            await locator.HoverAsync();
        }
    }
}
