using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing.Settings;

public interface IWebElementLocator
{
    Task<ElementPosition> DetectElementCoordinates(IWebBrowser browser, string contextId, string elementDescription);
}
