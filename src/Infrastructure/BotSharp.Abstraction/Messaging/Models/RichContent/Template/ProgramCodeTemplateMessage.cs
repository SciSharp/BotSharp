namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class ProgramCodeTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => RichTypeEnum.ProgramCode;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public virtual string TemplateType { get; set; } = TemplateTypeEnum.ProgramCode;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}
