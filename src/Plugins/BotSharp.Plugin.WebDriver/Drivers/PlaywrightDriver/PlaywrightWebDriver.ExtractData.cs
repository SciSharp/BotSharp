namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<string> ExtractData(Agent agent, BrowsingContextIn context, string messageId)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");
        var content = await body.InnerTextAsync();

        var driverService = _services.GetRequiredService<WebDriverService>();
        var answer = await driverService.ExtraData(agent, content, context.Question, messageId);

        return answer;
    }
}
