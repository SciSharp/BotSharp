namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class MultiSelectTemplateMessage : RichMessageBase, IRichMessage
    {
        public override string Type => "template";

        [JsonPropertyName("template_type")]
        public string TemplateType => "multi-select";
        public List<OptionElement> Options { get; set; } = new List<OptionElement>();
    }

    public class OptionElement
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Payload { get; set; }
    }
}