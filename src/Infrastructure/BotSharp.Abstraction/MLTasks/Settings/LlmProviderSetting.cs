namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmProviderSetting
{
    public string Provider { get; set; } 
        = "azure-openai";

    public List<LlmModelSetting> Models { get; set; } 
        = new List<LlmModelSetting>();

    public override string ToString()
    {
        return $"{Provider} with {Models.Count} models";
    }
}