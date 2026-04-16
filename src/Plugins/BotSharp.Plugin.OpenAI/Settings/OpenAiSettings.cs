namespace BotSharp.Plugin.OpenAI.Settings;

public class OpenAiSettings
{
    /// <summary>
    /// Defaults for the OpenAI web search tool (context size, approximate user location).
    /// Conversation state keys take precedence over these values at runtime.
    /// </summary>
    public WebSearchOptions? WebSearch { get; set; }
}
