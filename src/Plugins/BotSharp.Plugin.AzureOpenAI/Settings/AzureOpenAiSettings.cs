namespace BotSharp.Plugin.AzureOpenAI.Settings;

public class AzureOpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public GPT4Settings GPT4 { get; set; }
}
