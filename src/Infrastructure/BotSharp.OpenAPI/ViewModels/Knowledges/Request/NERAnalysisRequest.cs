namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class NERAnalysisRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public NEROptions? Options { get; set; }
}
