namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/send-messages/buttons
/// </summary>
public class ButtonTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => "button_template";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public string TemplateType => "button";

    [JsonPropertyName("buttons")]
    public ButtonElement[] Buttons { get; set; } = new ButtonElement[0];
}

public class ButtonElement
{
    /// <summary>
    /// web_url, postback, phone_number
    /// </summary>
    public string Type { get; set; } = "web_url";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Payload { get; set; }

    public string Title { get; set; } = string.Empty;
}
