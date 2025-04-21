namespace BotSharp.Abstraction.Conversations.Models;

public class TokenStatsModel
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Prompt { get; set; }
    public int TextInputTokens { get; set; }
    public int CachedTextInputTokens { get; set; }
    public int AudioInputTokens { get; set; }
    public int CachedAudioInputTokens { get; set; }
    public int TextOutputTokens { get; set; }
    public int AudioOutputTokens { get; set; }
    public AgentLlmConfig LlmConfig { get; set; }

    public int TotalInputTokens => TextInputTokens + CachedTextInputTokens + AudioInputTokens + CachedAudioInputTokens;
    public int TotalOutputTokens => TextOutputTokens + AudioOutputTokens;
}
