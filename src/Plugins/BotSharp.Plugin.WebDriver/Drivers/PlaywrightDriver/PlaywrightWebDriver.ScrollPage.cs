namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> ScrollPage(MessageInfo message, PageActionArgs args)
    {
        var result = new BrowserActionResult();
        await _instance.Wait(message.ContextId);

        var page = _instance.GetPage(message.ContextId);

        if (args.Direction == "down")
        {
            // Get the total page height
            int scrollY = await page.EvaluateAsync<int>("window.screen.height");

            // Scroll a page down
            await page.Mouse.WheelAsync(0, scrollY);
        }
        else if (args.Direction == "up")
        {
            // Get the total page height
            int scrollY = await page.EvaluateAsync<int>("window.screen.height");

            // Scroll a page up
            await page.Mouse.WheelAsync(0, -scrollY);
        }
        else if (args.Direction == "bottom")
        {
            // Get the total page height
            int scrollY = await page.EvaluateAsync<int>("document.body.scrollHeight");

            // Scroll to the bottom
            await page.Mouse.WheelAsync(0, -scrollY);
        }
        else if (args.Direction == "top")
        {
            // Get the total page height
            int scrollY = await page.EvaluateAsync<int>("document.body.scrollHeight");

            // Scroll to the top
            await page.Mouse.WheelAsync(0, -scrollY);
        }
        else if (args.Direction == "left")
        {
            await page.EvaluateAsync(@"
                var scrollingElement = (document.scrollingElement || document.body);
                scrollingElement.scrollLeft = 0;
            ");
        }
        else if (args.Direction == "right")
        {
            await page.EvaluateAsync(@"
                var scrollingElement = (document.scrollingElement || document.body);
                scrollingElement.scrollLeft = scrollingElement.scrollWidth;
            ");
        }

        result.IsSuccess = true;
        return result;
    }
}
