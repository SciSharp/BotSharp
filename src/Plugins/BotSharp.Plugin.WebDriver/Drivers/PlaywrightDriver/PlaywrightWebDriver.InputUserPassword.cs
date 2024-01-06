using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task InputUserPassword(Agent agent, BrowsingContextIn context, string messageId)
    {
        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var inputs = await body.QuerySelectorAllAsync("input");
        var password = inputs.FirstOrDefault(x => x.GetAttributeAsync("type").Result == "password");

        if (password == null)
        {
            throw new Exception($"Can't locate the web element {context.ElementName}.");
        }

        var config = _services.GetRequiredService<IConfiguration>();
        try
        {
            var key = context.Password.Replace("@", "").Replace(".", ":");
            var value = config.GetValue<string>(key);
            await password.FillAsync(value);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
