namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result)
    {
        var page = _instance.GetPage(message.ContextId);
        if (string.IsNullOrEmpty(result.Selector))
        {
            Serilog.Log.Error($"Selector is not set.");
            return;
        }

        ILocator locator = page.Locator(result.Selector);
        var count = await locator.CountAsync();

        if (count == 0)
        {
            Serilog.Log.Error($"Element not found: {result.Selector}");
            return;
        }
        else if (count > 1)
        {
            if(!action.FirstIfMultipleFound)
            {
                Serilog.Log.Error($"Multiple eElements were found: {result.Selector}");
                return;
            }
            else
            {
                locator = page.Locator(result.Selector).First;// 匹配到多个时取第一个，否则当await locator.ClickAsync();匹配到多个就会抛异常。
            }
        }

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
                if (action.DelayBeforePressingKey > 0)
                {
                    await Task.Delay(action.DelayBeforePressingKey);
                }
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Typing)
        {
            await locator.PressSequentiallyAsync(action.Content);
            if (action.PressKey != null)
            {
                if (action.DelayBeforePressingKey > 0)
                {
                    await Task.Delay(action.DelayBeforePressingKey);
                }
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Hover)
        {
            await locator.HoverAsync();
        }

        if (action.WaitTime > 0)
        {
            await Task.Delay(1000 * action.WaitTime);
        }
    }
}
