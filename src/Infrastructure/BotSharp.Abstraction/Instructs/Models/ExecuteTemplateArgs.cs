namespace BotSharp.Abstraction.Instructs.Models;

public class ExecuteTemplateArgs
{
    [JsonPropertyName("template_name")]
    public string? TemplateName { get; set; }
}
