using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Instructs.Models;

public class InstructBaseRequest
{
    [JsonPropertyName("provider")]
    public virtual string? Provider { get; set; } = null;

    [JsonPropertyName("model")]
    public virtual string? Model { get; set; } = null;

    [JsonPropertyName("model_id")]
    public virtual string? ModelId { get; set; } = null;

    [JsonPropertyName("states")]
    public List<MessageState> States { get; set; } = new();
}

public class MultiModalRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public List<InstructFileModel> Files { get; set; } = new();
}

public class ImageGenerationRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class ImageVariationRequest : InstructBaseRequest
{
    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }
}

public class ImageEditRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }
}

public class ImageMaskEditRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }

    [JsonPropertyName("mask")]
    public InstructFileModel Mask { get; set; }
}

public class SpeechToTextRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }
}

public class TextToSpeechRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}