namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class ResponseAudioTranscript : ServerEventResponse
{
    [JsonPropertyName("response_id")]
    public string ResponseId { get; set; } = null!;

    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = null!;

    [JsonPropertyName("output_index")]
    public int OutputIndex { get; set; }

    [JsonPropertyName("content_index")]
    public int ContentIndex { get; set; }

    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }
}
