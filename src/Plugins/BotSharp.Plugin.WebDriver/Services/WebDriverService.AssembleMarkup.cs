namespace BotSharp.Plugin.WebDriver.Services;

public partial class WebDriverService
{
    internal string AssembleMarkup(string tagName, MarkupProperties properties)
    {
        var html = $"<{tagName}";
        if (!string.IsNullOrEmpty(properties.Id))
        {
            html += $" id=\"{properties.Id}\"";
        }

        if (!string.IsNullOrEmpty(properties.Name))
        {
            html += $" name=\"{properties.Name}\"";
        }

        if (!string.IsNullOrEmpty(properties.Type))
        {
            html += $" type=\"{properties.Type}\"";
        }

        if (!string.IsNullOrEmpty(properties.Placeholder))
        {
            html += $" placeholder=\"{properties.Placeholder}\"";
        }

        if (!string.IsNullOrEmpty(properties.Text))
        {
            html += $">{properties.Text}</{tagName}>";
            return html;
        }

        return html + $"></{tagName}>";
    }
}
