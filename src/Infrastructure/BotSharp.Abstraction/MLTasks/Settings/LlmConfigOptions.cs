namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmConfigOptions
{
    public LlmModelType? Type { get; set; }
    public bool? MultiModal { get; set; }
    public bool? ImageGeneration { get; set; }
}
