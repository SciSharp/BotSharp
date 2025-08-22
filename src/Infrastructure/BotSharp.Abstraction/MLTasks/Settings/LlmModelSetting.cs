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
    public LlmModelType Type { get; set; } = LlmModelType.Chat;

    /// <summary>
    /// If true, allow sending images/vidoes to this model
    /// </summary>
    public bool MultiModal { get; set; }

    /// <summary>
    /// If true, allow generating images
    /// </summary>
    public bool ImageGeneration { get; set; }

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
    /// Settings for llm cost
    /// </summary>
    public LlmCostSetting Cost { get; set; } = new();

    public override string ToString()
    {
        return $"[{Type}] {Name} {Endpoint}";
    }
}

#region Embedding model settings
public class EmbeddingSetting
{
    public bool IsDefault { get; set; }
    public int Dimension { get; set; }
}
#endregion


#region Reasoning model settings
public class ReasoningSetting
{
    public bool IsDefault { get; set; }
    public float Temperature { get; set; } = 1.0f;
    public string? EffortLevel { get; set; }
}
#endregion

#region Web search model settings
public class WebSearchSetting
{
    public bool IsDefault { get; set; }
    public string? SearchContextSize { get; set; }
}
#endregion


#region Image model settings
public class ImageSetting
{
    public ImageGenerationSetting? Generation { get; set; }
    public ImageEditSetting? Edit { get; set; }
    public ImageVariationSetting? Variation { get; set; }
}

public class ImageGenerationSetting
{
    public bool IsDefault { get; set; }
    public ModelSettingBase? Style { get; set; }
    public ModelSettingBase? Size { get; set; }
    public ModelSettingBase? Quality { get; set; }
    public ModelSettingBase? ResponseFormat { get; set; }
}

public class ImageEditSetting
{
    public bool IsDefault { get; set; }
    public ModelSettingBase? Size { get; set; }
    public ModelSettingBase? ResponseFormat { get; set; }
}

public class ImageVariationSetting
{
    public bool IsDefault { get; set; }
    public ModelSettingBase? Size { get; set; }
    public ModelSettingBase? ResponseFormat { get; set; }
}
#endregion

public class ModelSettingBase
{
    public string? Default { get; set; }
    public IEnumerable<string>? Options { get; set; }
}


/// <summary>
/// Cost per 1K tokens
/// </summary>
public class LlmCostSetting
{
    // Input
    public float TextInputCost { get; set; } = 0f;
    public float CachedTextInputCost { get; set; } = 0f;
    public float AudioInputCost { get; set; } = 0f;
    public float CachedAudioInputCost { get; set; } = 0f;

    // Output
    public float TextOutputCost { get; set; } = 0f;
    public float AudioOutputCost { get; set; } = 0f;
}

public enum LlmModelType
{
    Text = 1,
    Chat = 2,
    Image = 3,
    Embedding = 4,
    Audio = 5,
    Realtime = 6,
}
