namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmProviderSetting
{
    public string Provider { get; set; } = "azure-openai";

    public List<LlmModelSetting> Models { get; set; } = [];

    /// <summary>
    /// A copy whose models carry no api key or endpoint, for listing to clients
    /// </summary>
    public LlmProviderSetting WithoutCredentials()
    {
        var copy = (LlmProviderSetting)MemberwiseClone();
        copy.Models = Models?.Select(x => x.WithoutCredentials()).ToList() ?? [];
        return copy;
    }

    public override string ToString()
    {
        return $"{Provider} with {Models.Count} models";
    }
}