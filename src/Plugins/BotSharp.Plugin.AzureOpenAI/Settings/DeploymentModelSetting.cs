namespace BotSharp.Plugin.AzureOpenAI.Settings;

public class DeploymentModelSetting
{
    public string? ChatCompletionModel { get; set; }
    public string? TextCompletionModel { get; set; }

    public override string ToString()
    {
        return $"ChatCompletion - {ChatCompletionModel}, TextCompletion - {TextCompletionModel}";
    }
}
