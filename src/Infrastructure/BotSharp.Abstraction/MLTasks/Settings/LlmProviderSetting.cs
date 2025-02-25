namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmProviderSetting
{
    public string Provider { get; set; } = "azure-openai";

    public List<LlmModelSetting> Models { get; set; } = [];

    public override string ToString()
    {
        return $"{Provider} with {Models.Count} models";
    }
}