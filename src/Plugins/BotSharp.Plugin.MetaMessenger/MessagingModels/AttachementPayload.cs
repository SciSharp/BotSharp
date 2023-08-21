using System.Text.Json.Serialization;

namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

public class AttachementPayload
{
    [JsonPropertyName("template_type")]
    public string TemplateType { get; set; }
    public string Text { get; set; }
    public ButtonItem[] Buttons { get; set; }
}
