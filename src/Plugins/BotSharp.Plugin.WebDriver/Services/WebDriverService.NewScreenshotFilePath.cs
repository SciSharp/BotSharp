using System.IO;

namespace BotSharp.Plugin.WebDriver.Services;

public partial class WebDriverService
{
    public string NewScreenshotFilePath(string messageId)
    {
        var conversation = _services.GetRequiredService<IConversationService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var dir = $"{agentService.GetDataDir()}/conversations/{conversation.ConversationId}/screenshots";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return $"{dir}/{messageId}.png";
    }
}
