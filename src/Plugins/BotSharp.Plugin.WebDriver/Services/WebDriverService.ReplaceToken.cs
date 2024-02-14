using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Services;

public partial class WebDriverService
{
    /// <summary>
    /// Replace token started @ with settings.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public string ReplaceToken(string text)
    {
        var config = _services.GetRequiredService<IConfiguration>();
        var token = Regex.Match(text, "@[a-zA-Z0-9._]+");
        if (token.Success)
        {
            var key = token.Value.Replace("@", "").Replace(".", ":");
            var value = config.GetValue<string>(key);
            return text.Replace(token.Value, value);
        }
        return text;
    }
}
