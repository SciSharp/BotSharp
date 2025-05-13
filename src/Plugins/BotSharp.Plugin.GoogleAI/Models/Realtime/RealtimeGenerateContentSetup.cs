using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAI.Models.Realtime;

internal class RealtimeGenerateContentSetup
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("generationConfig")]
    public GenerationConfig? GenerationConfig { get; set; }

    [JsonPropertyName("systemInstruction")]
    public Content? SystemInstruction { get; set; }

    [JsonPropertyName("tools")]
    public Tool[]? Tools { get; set; }

    [JsonPropertyName("inputAudioTranscription")]
    public AudioTranscriptionConfig? InputAudioTranscription { get; set; }

    [JsonPropertyName("outputAudioTranscription")]
    public AudioTranscriptionConfig? OutputAudioTranscription { get; set; }
}

internal class AudioTranscriptionConfig { }