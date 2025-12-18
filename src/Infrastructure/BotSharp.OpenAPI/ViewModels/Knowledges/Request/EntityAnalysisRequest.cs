namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class EntityAnalysisRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public EntityAnalysisOptions? Options { get; set; }
}
