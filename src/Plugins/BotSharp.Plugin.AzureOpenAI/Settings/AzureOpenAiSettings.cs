namespace BotSharp.Plugin.AzureOpenAI.Settings;

public class AzureOpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public DeploymentModelSetting DeploymentModel { get; set; } 
        = new DeploymentModelSetting();
    public string InstructionFile { get; set; } = string.Empty;
    public string ChatSampleFile { get; set; } = string.Empty;
}
