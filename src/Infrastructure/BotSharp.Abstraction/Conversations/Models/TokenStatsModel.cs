namespace BotSharp.Abstraction.Conversations.Models;

public class TokenStatsModel
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Prompt { get; set; }

    #region Input
    public int TextInputTokens { get; set; }
    public int CachedTextInputTokens { get; set; }
    public int AudioInputTokens { get; set; }
    public int CachedAudioInputTokens { get; set; }
    #endregion

    #region Output
    public int TextOutputTokens { get; set; }
    public int AudioOutputTokens { get; set; }
    #endregion


    public int TotalInputTokens => TextInputTokens + CachedTextInputTokens + AudioInputTokens + CachedAudioInputTokens;
    public int TotalOutputTokens => TextOutputTokens + AudioOutputTokens;
}
