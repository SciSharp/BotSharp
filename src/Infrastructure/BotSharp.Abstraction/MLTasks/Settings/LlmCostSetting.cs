namespace BotSharp.Abstraction.MLTasks.Settings;

/// <summary>
/// Cost per 1K tokens
/// </summary>
public class LlmCostSetting
{
    #region Text token
    public float TextInputCost { get; set; } = 0f;
    public float CachedTextInputCost { get; set; } = 0f;
    public float CachedTextInputWriteCost { get; set; } = 0f;
    public float TextOutputCost { get; set; } = 0f;
    public string DefaultServiceTier { get; set; } = "standard";
    public IList<LlmTextTokenCostTier>? TextTokenCostTiers { get; set; }
    #endregion

    #region Audio token
    public float AudioInputCost { get; set; } = 0f;
    public float CachedAudioInputCost { get; set; } = 0f;
    public float AudioOutputCost { get; set; } = 0f;
    #endregion

    #region Image token
    public float ImageInputCost { get; set; } = 0f;
    public float CachedImageInputCost { get; set; } = 0f;
    public float ImageOutputCost { get; set; } = 0f;
    #endregion

    #region Image
    public IList<LlmImageCost>? ImageCosts { get; set; }
    #endregion

    public LlmTextTokenCostTier? GetTextTokenCostTier(long inputTokens, string? serviceTier = null)
    {
        var selectedServiceTier = string.IsNullOrWhiteSpace(serviceTier)
            ? DefaultServiceTier
            : serviceTier;

        return TextTokenCostTiers?.FirstOrDefault(x =>
            string.Equals(x.ServiceTier, selectedServiceTier, StringComparison.OrdinalIgnoreCase)
            && (!x.InputTokensGreaterThan.HasValue || inputTokens > x.InputTokensGreaterThan.Value)
            && (!x.InputTokensLessThanOrEqual.HasValue || inputTokens <= x.InputTokensLessThanOrEqual.Value));
    }
}

public class LlmTextTokenCostTier
{
    public string ServiceTier { get; set; } = "standard";
    public long? InputTokensGreaterThan { get; set; }
    public long? InputTokensLessThanOrEqual { get; set; }
    public float TextInputCost { get; set; }
    public float CachedTextInputCost { get; set; }
    public float CachedTextInputWriteCost { get; set; }
    public float TextOutputCost { get; set; }
}

public class LlmImageCost
{
    /// <summary>
    /// Attributes: e.g., [quality]: "medium", [size] = "1024x1024"
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = [];
    public float Cost { get; set; } = 0f;
}