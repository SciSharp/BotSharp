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
            _logger.LogError($"Can't locate the password element by '{actionParams.Context.ElementName}'");
            return false;
        }

        var config = _services.GetRequiredService<IConfiguration>();
        try
        {
            await password.FillAsync(actionParams.Context.Password);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        return false;
    }
}
