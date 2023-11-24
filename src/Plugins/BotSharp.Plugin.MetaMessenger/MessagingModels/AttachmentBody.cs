namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

public class AttachmentBody
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "template";
    public ITemplateMessage Payload { get; set; }
}
