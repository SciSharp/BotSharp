namespace BotSharp.Plugin.MetaMessenger.WebhookModels;

public class WebhookMessagePostback
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Title { get; set; }

    public string Payload { get; set; }

    [JsonPropertyName("mid")]
    public string Id { get; set; }
}
