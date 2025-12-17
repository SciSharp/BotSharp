namespace BotSharp.Abstraction.Conversations.Models;

public class TokenStatsModel
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Prompt { get; set; }

    #region Text token
    public int TextInputTokens { get; set; }
    public int CachedTextInputTokens { get; set; }
    public int TextOutputTokens { get; set; }
    #endregion

    #region Audio token
    public int AudioInputTokens { get; set; }
    public int CachedAudioInputTokens { get; set; }
    public int AudioOutputTokens { get; set; }
    #endregion

    #region Image token
    public int ImageInputTokens { get; set; }
    public int CachedImageInputTokens { get; set; }
    public int ImageOutputTokens { get; set; }
    #endregion

    #region Image
    public int ImageGenerationCount { get; set; }
    public float ImageGenerationUnitCost { get; set; }
    #endregion

    public int TotalInputTokens => TextInputTokens + CachedTextInputTokens 
                                + AudioInputTokens + CachedAudioInputTokens
                                + ImageInputTokens + CachedImageInputTokens;
    public int TotalOutputTokens => TextOutputTokens + AudioOutputTokens + ImageOutputTokens;
}
