namespace BotSharp.Plugin.OpenAI.Models.Web;

/// <summary>
/// Plugin-level configuration for the OpenAI web search tool.
/// Bound from the "OpenAi:WebSearch" configuration section via <see cref="OpenAiSettings"/>.
/// </summary>
public class WebSearchOptions
{
    /// <summary>
    /// Default context size ("low", "medium", "high") used when no conversation state override is set.
    /// </summary>
    public string? SearchContextSize { get; set; }

    /// <summary>
    /// Default approximate user location used when no conversation state override is set.
    /// </summary>
    public WebSearchUserLocation? UserLocation { get; set; }
}
