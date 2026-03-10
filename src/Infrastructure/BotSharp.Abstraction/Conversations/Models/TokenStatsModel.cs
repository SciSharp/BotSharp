namespace BotSharp.Abstraction.Conversations.Models;

public class TokenStatsModel
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Prompt { get; set; }

    #region Text token
    public long TextInputTokens { get; set; }
    public long CachedTextInputTokens { get; set; }
    public long TextOutputTokens { get; set; }
    #endregion

    #region Audio token
    public int AudioInputTokens { get; set; }
    public int CachedAudioInputTokens { get; set; }
    public int AudioOutputTokens { get; set; }
    #endregion

    #region Image token
    public long ImageInputTokens { get; set; }
    public long CachedImageInputTokens { get; set; }
    public long ImageOutputTokens { get; set; }
    #endregion

    #region Image
    public int ImageGenerationCount { get; set; }
    public float ImageGenerationUnitCost { get; set; }
    #endregion

    public long TotalInputTokens => TextInputTokens + CachedTextInputTokens 
                                + AudioInputTokens + CachedAudioInputTokens
                                + ImageInputTokens + CachedImageInputTokens;
    public long TotalOutputTokens => TextOutputTokens + AudioOutputTokens + ImageOutputTokens;
}
