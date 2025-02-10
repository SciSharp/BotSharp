namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class ResponseDone : ServerEventResponse
{
    [JsonPropertyName("response")]
    public ResponseDoneBody Body { get; set; } = new();
}

public class ResponseDoneBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("object")]
    public string Object { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("status_details")]
    public ResponseDoneStatusDetail StatusDetails { get; set; } = new();

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = null!;

    [JsonPropertyName("usage")]
    public ModelTokenUsage Usage { get; set; } = new();

    [JsonPropertyName("modalities")]
    public string[] Modalities { get; set; } = [];

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [JsonPropertyName("output_audio_format")]
    public string OutputAudioFormat { get; set; } = null!;

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = null!;

    [JsonPropertyName("output")]
    public ModelResponseDoneOutput[] Outputs { get; set; } = [];
}

public class ModelTokenUsage
{
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

public class ModelResponseDoneOutput
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("object")]
    public string Object { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = null!;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = null!;

    [JsonPropertyName("content")]
    public ResponseDoneOutputContent[] Content { get; set; } = [];
}

public class ResponseDoneStatusDetail
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = null!;
}

public class ResponseDoneOutputContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("transcript")]
    public string Transcript { get; set; } = null!;
}