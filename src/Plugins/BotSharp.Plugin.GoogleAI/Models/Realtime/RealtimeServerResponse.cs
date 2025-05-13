using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAI.Models.Realtime;

internal class RealtimeServerResponse
{
    [JsonPropertyName("setupComplete")]
    public RealtimeGenerateContentSetupComplete? SetupComplete { get; set; }

    [JsonPropertyName("serverContent")]
    public RealtimeGenerateContentServerContent? ServerContent { get; set; }

    [JsonPropertyName("usageMetadata")]
    public RealtimeUsageMetaData? UsageMetaData { get; set; }
}


internal class RealtimeGenerateContentSetupComplete { }

internal class RealtimeGenerateContentServerContent
{
    [JsonPropertyName("turnComplete")]
    public bool? TurnComplete { get; set; }

    [JsonPropertyName("generationComplete")]
    public bool? GenerationComplete { get; set; }

    [JsonPropertyName("interrupted")]
    public bool? Interrupted { get; set; }

    [JsonPropertyName("modelTurn")]
    public Content? ModelTurn { get; set; }

    [JsonPropertyName("inputTranscription")]
    public RealtimeGenerateContentTranscription? InputTranscription { get; set; }

    [JsonPropertyName("outputTranscription")]
    public RealtimeGenerateContentTranscription? OutputTranscription { get; set; }
}

internal class RealtimeUsageMetaData
{
    [JsonPropertyName("promptTokenCount")]
    public int? PromptTokenCount { get; set; }

    [JsonPropertyName("responseTokenCount")]
    public int? ResponseTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int? TotalTokenCount { get; set; }

    [JsonPropertyName("promptTokensDetails")]
    public List<RealtimeTokenDetail>? PromptTokensDetails { get; set; }

    [JsonPropertyName("responseTokensDetails")]
    public List<RealtimeTokenDetail>? ResponseTokensDetails { get; set; }
}


internal class RealtimeTokenDetail
{
    [JsonPropertyName("modality")]
    public string? Modality { get; set; }

    [JsonPropertyName("tokenCount")]
    public int? TokenCount { get; set; }
}

internal class RealtimeGenerateContentTranscription
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}