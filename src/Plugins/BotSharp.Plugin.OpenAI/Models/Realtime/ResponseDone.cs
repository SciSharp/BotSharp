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

    [JsonPropertyName("input_token_details")]
    public InputTokenDetail? InputTokenDetails { get; set; }

    [JsonPropertyName("output_token_details")]
    public OutputTokenDetail? OutputTokenDetails { get; set; }
}

public class InputTokenDetail
{
    [JsonPropertyName("text_tokens")]
    public int? TextTokens { get; set; }

    [JsonPropertyName("audio_tokens")]
    public int? AudioTokens { get; set; }

    [JsonPropertyName("cached_tokens")]
    public int? CachedTokens { get; set; }

    [JsonPropertyName("cached_tokens_details")]
    public CachedTokenDetail? CachedTokenDetails { get; set; }
}

public class CachedTokenDetail
{
    [JsonPropertyName("text_tokens")]
    public int? TextTokens { get; set; }

    [JsonPropertyName("audio_tokens")]
    public int? AudioTokens { get; set; }
}

public class OutputTokenDetail
{
    [JsonPropertyName("text_tokens")]
    public int? TextTokens { get; set; }

    [JsonPropertyName("audio_tokens")]
    public int? AudioTokens { get; set; }
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
    public string? Reason { get; set; } = null!;

    [JsonPropertyName("error")]
    public ResponseDoneErrorStatus? Error { get; set; } = null!;

    public override string ToString()
    {
        return $"{Type}: {Reason} ({Error})";
    }
}

public class ResponseDoneErrorStatus
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("message")]
    public string? Message { get; set; } = null!;

    [JsonPropertyName("code")]
    public string? Code { get; set; } = null!;

    public override string ToString()
    {
        return $"{Type}: {Message} ({Code})";
    }
}

public class ResponseDoneOutputContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("transcript")]
    public string Transcript { get; set; } = null!;
}