using BotSharp.Abstraction.Messaging.Enums;

namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/send-messages/buttons
/// </summary>
public class ButtonTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.ButtonTemplate;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public string TemplateType => TemplateTypeEnum.Button;

    [JsonPropertyName("buttons")]
    public ElementButton[] Buttons { get; set; } = new ElementButton[0];

    [JsonPropertyName("is_horizontal")]
    public bool IsHorizontal { get; set; }
}
