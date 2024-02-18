namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<string> ExtractData(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        await Task.Delay(3000);

        // Retrieve the page raw html and infer the element path
        var body = await _instance.GetPage(actionParams.ConversationId).QuerySelectorAsync("body");
        var content = await body.InnerTextAsync();

        var driverService = _services.GetRequiredService<WebDriverService>();
        var answer = await driverService.ExtraData(actionParams.Agent, content, actionParams.Context.Question, actionParams.MessageId);

        return answer;
    }
}
