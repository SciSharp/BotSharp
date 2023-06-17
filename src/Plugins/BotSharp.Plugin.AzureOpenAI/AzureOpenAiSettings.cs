namespace BotSharp.Platform.AzureAi;

public class AzureOpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string InstructionFile { get; set; } = string.Empty;
    public string ChatSampleFile { get; set; } = string.Empty;
}
