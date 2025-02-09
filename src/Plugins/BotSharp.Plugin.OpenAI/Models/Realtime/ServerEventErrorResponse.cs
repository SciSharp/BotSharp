namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class ServerEventErrorResponse : ServerEventResponse
{
    [JsonPropertyName("error")]
    public ServerEventErrorBody Body { get; set; } = new();
}

public class ServerEventErrorBody
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
