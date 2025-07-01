namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class JsCodeTemplateMessage : IRichMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.JsCode;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
