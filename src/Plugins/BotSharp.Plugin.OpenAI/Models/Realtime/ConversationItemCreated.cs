namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class ConversationItemCreated : ServerEventResponse
{
    [JsonPropertyName("item")]
    public ConversationItemBody Item { get; set; } = new();
}

public class ConversationItemBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("role")]
    public string Role { get; set;} = null!;

    [JsonPropertyName("content")]
    public ConversationItemContent[] Content { get; set; } = [];
}

public class ConversationItemContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("transcript")]
    public string Transcript { get; set; } = null!;

    [JsonPropertyName("audio")]
    public string Audio { get; set; } = null!;
}
