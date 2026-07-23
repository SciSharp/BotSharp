namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmModelSetting
{
    /// <summary>
    /// Model Id, like "gpt-4", "gpt-4o", "o1".
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Deployment model name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Model version
    /// </summary>
    public string Version { get; set; } = "1106-Preview";

    /// <summary>
    /// Api version
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Deployment same functional model in a group.
    /// It can be used to deploy same model in different regions.
    /// </summary>
    public string? Group { get; set; }

    public string ApiKey { get; set; } = null!;
    public string? Endpoint { get; set; }
    public long? ContextWindow { get; set; }
    public LlmModelType Type { get; set; } = LlmModelType.Chat;
    public List<LlmModelCapability> Capabilities { get; set; } = [];
    public List<string> InputModalities { get; set; } = [];

    /// <summary>
    /// If true, allow sending images/videos to this model
    /// </summary>
    public bool MultiModal { get; set; }

    /// <summary>
    /// Settings for embedding
    /// </summary>
    public EmbeddingSetting? Embedding { get; set; }

    /// <summary>
    /// Settings for reasoning model
    /// </summary>
    public ReasoningSetting? Reasoning { get; set; }

    /// <summary>
    /// Settings for web search
    /// </summary>
    public WebSearchSetting? WebSearch { get; set; }

    /// <summary>
    /// Settings for images
    /// </summary>
    public ImageSetting? Image { get; set; }

    /// <summary>
    /// Settings for audio
    /// </summary>
    public AudioSetting? Audio { get; set; }

    /// <summary>
    /// Settings for llm cost
    /// </summary>
    public LlmCostSetting Cost { get; set; } = new();

    public bool AllowPdfReading => Capabilities?.Contains(LlmModelCapability.PdfReading) == true;

    public override string ToString()
    {
        return $"[{Type}] {Name} {Endpoint}";
    }
}

#region Embedding model settings
public class EmbeddingSetting
{
    public int Dimension { get; set; }
}
#endregion


#region Reasoning model settings
public class ReasoningSetting
{
    public float? Temperature { get; set; }

    [Obsolete("Set EffortLevel in Parameters")]
    public string? EffortLevel { get; set; }
    public Dictionary<string, ModelParamSetting>? Parameters { get; set; }
}
#endregion

#region Web search model settings
public class WebSearchSetting
{
    public bool IsDefault { get; set; }

    [Obsolete("Set SearchContextSize in Parameters")]
    public string? SearchContextSize { get; set; }
    public Dictionary<string, ModelParamSetting>? Parameters { get; set; }
}
#endregion

#region Image model settings
public class ImageSetting
{
    public ImageGenerationSetting? Generation { get; set; }
    public ImageEditSetting? Edit { get; set; }
    public ImageVariationSetting? Variation { get; set; }
}

/// <summary>
/// Image generationg setting
/// </summary>
public class ImageGenerationSetting
{
    /// <summary>
    /// Parameters: quality, size, style, background, response format
    /// </summary>
    public Dictionary<string, ModelParamSetting>? Parameters { get; set; }
}

/// <summary>
/// Image editing setting
/// </summary>
public class ImageEditSetting
{
    /// <summary>
    /// Parameters: quality, size, background, response format
    /// </summary>
    public Dictionary<string, ModelParamSetting>? Parameters { get; set; }
}

/// <summary>
/// Image variation setting
/// </summary>
public class ImageVariationSetting
{
    /// <summary>
    /// Parameters: size, response format
    /// </summary>
    public Dictionary<string, ModelParamSetting>? Parameters { get; set; }
}
#endregion

#region Audio model settings
public class AudioSetting
{
    public AudioTranscriptionSetting? Transcription { get; set; }
}

/// <summary>
/// Audio transcription setting
/// </summary>
public class AudioTranscriptionSetting
{
    public float? Temperature { get; set; }

    /// <summary>
    /// Parameters: granularity, response format
    /// </summary>
    public Dictionary<string, ModelParamSetting>? Parameters { get; set; }
}
#endregion

public class ModelParamSetting
{
    public string? Default { get; set; }
    public IEnumerable<string>? Options { get; set; }
}


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

public enum LlmModelType
{
    All = 0,
    Text = 1,
    Chat = 2,
    Image = 3,
    Embedding = 4,
    Audio = 5,
    Realtime = 6,
    Web = 7
}

public enum LlmModelCapability
{
    All = 0,
    Text = 1,
    Chat = 2,
    ImageReading = 3,
    ImageGeneration = 4,
    ImageEdit = 5,
    ImageVariation = 6,
    ImageComposition = 7,
    Embedding = 8,
    AudioTranscription = 9,
    AudioGeneration = 10,
    Realtime = 11,
    WebSearch = 12,
    PdfReading = 13
}
