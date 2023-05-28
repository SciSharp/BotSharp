namespace BotSharp.Platform.AzureAi;

public class AzureAiSettings
{
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
    public string DeploymentModel { get; set; }
    public string InstructionFile { get; set; }
}
