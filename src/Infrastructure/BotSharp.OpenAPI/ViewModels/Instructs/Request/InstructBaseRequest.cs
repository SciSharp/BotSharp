using BotSharp.OpenAPI.ViewModels.Instructs.Request;
using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Instructs.Models;

public class InstructBaseRequest
{
    [JsonPropertyName("provider")]
    public virtual string? Provider { get; set; } = null;

    [JsonPropertyName("model")]
    public virtual string? Model { get; set; } = null;

    [JsonPropertyName("agent_id")]
    public virtual string? AgentId { get; set; }

    [JsonPropertyName("template_name")]
    public virtual string? TemplateName { get; set; }

    [JsonPropertyName("states")]
    public List<InstructState> States { get; set; } = [];
}


public class MultiModalRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class MultiModalFileRequest : MultiModalRequest
{
    [JsonPropertyName("files")]
    public List<InstructFileModel> Files { get; set; } = [];
}


public class ImageGenerationRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}


public class ImageVariationRequest : InstructBaseRequest
{
    [JsonPropertyName("image_converter")]
    public string? ImageConverter { get; set; }
}

public class ImageVariationFileRequest : ImageVariationRequest
{
    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }
}


public class ImageEditRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("image_converter")]
    public string? ImageConverter { get; set; }
}

public class ImageEditFileRequest : ImageEditRequest
{
    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }
}

public class ImageCompositionRequest : ImageEditRequest
{
    [JsonPropertyName("files")]
    public InstructFileModel[] Files { get; set; } = [];
}

public class ImageMaskEditRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("image_converter")]
    public string? ImageConverter { get; set; }
}

public class ImageMaskEditFileRequest : ImageMaskEditRequest
{
    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }

    [JsonPropertyName("mask")]
    public InstructFileModel Mask { get; set; }
}


public class PdfReadRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("image_converter")]
    public string? ImageConverter { get; set; }
}

public class PdfReadFileRequest : PdfReadRequest
{
    [JsonPropertyName("files")]
    public List<InstructFileModel> Files { get; set; } = [];
}


public class SpeechToTextRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class SpeechToTextFileRequest : SpeechToTextRequest
{
    [JsonPropertyName("file")]
    public InstructFileModel File { get; set; }
}

public class TextToSpeechRequest : InstructBaseRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}