namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<string> ExtractData(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ContextId);

        await Task.Delay(3000);

        // Retrieve the page raw html and infer the element path
        var pageContent = _instance.GetPage(actionParams.ContextId);
        var body = await pageContent.QuerySelectorAsync("body");
        var pageUrl = pageContent.Url;
        var pageBody = await body.InnerTextAsync();
        string content = $"Page URL: `{pageUrl}` <br/> PageBody: {pageBody}";

        var driverService = _services.GetRequiredService<WebDriverService>();
        var answer = await driverService.ExtraData(actionParams.Agent, content, actionParams.Context.Question, actionParams.MessageId);

        return answer;
    }
}
