namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

public class AttachmentBody
{
    public string Type { get; set; } = "template";
    public AttachementPayload Payload { get; set; }
}
