namespace BotSharp.Plugin.OpenAI.Models.Live;

public static class LiveModelConstants
{
    /// <summary>
    /// Full duplex voice model served from the v1/live/sessions endpoint.
    /// </summary>
    public const string GPT_Live_1 = "gpt-live-1";

    public const string Endpoint = "wss://api.openai.com/v1/live/sessions";
}
