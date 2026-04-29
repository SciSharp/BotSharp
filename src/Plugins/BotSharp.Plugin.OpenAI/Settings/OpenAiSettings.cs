namespace BotSharp.Plugin.OpenAI.Settings;

public class OpenAiSettings
{
    /// <summary>
    /// Switch on to use response api; Switch off to use legacy chat completion
    /// </summary>
    public bool UseResponseApi { get; set; }

    /// <summary>
    /// Defaults for the OpenAI web search tool (context size, approximate user location).
    /// Conversation state keys take precedence over these values at runtime.
    /// </summary>
    public WebSearchSettings? WebSearch { get; set; }
}
