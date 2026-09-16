namespace BotSharp.Plugin.OpenAI.Models.Live;

public static class LiveModelConstants
{
    /// <summary>
    /// Full duplex voice model served from the v1/live/sessions endpoint.
    /// </summary>
    public const string GPT_Live_1 = "gpt-live-1";

    /// <summary>
    /// Provider key used to select the Live completer, kept apart from "openai" so that
    /// the realtime and live providers can be registered side by side.
    /// </summary>
    public const string Provider = "openai-live";

    /// <summary>
    /// Provider whose credentials are reused when no "openai-live" section is configured.
    /// </summary>
    public const string FallbackSettingsProvider = "openai";

    public const string Endpoint = "wss://api.openai.com/v1/live/sessions";
}
