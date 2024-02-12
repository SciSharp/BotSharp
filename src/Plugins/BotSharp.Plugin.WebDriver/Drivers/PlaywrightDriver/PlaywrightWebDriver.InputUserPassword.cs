using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> InputUserPassword(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        // Retrieve the page raw html and infer the element path
        var body = await _instance.GetPage(actionParams.ConversationId)
            .QuerySelectorAsync("body");

        var inputs = await body.QuerySelectorAllAsync("input");
        var password = inputs.FirstOrDefault(x => x.GetAttributeAsync("type").Result == "password");

        if (password == null)
        {
            throw new Exception($"Can't locate the web element {actionParams.Context.ElementName}.");
        }

        var config = _services.GetRequiredService<IConfiguration>();
        try
        {
            var key = actionParams.Context.Password.Replace("@", "").Replace(".", ":");
            var value = config.GetValue<string>(key);
            await password.FillAsync(value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        return false;
    }
}
