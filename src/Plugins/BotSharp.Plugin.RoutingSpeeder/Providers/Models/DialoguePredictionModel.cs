namespace BotSharp.Plugin.RoutingSpeeder.Providers.Models;

public class DialoguePredictionModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Prediction { get; set; }
}
