using System.IO;

namespace BotSharp.Plugin.WebDriver.Services;

public partial class WebDriverService
{
    public string GetScreenshotDir()
    {
        var conversation = _services.GetRequiredService<IConversationService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var dir = $"{agentService.GetDataDir()}/conversations/{conversation.ConversationId}/screenshots";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public string GetScreenshotFilePath(string messageId)
    {
        var dir = GetScreenshotDir();
        return $"{dir}/{messageId}.png";
    }

    public string? GetScreenshotBase64(string messageId)
    {
        var filePath = GetScreenshotFilePath(messageId);
        if (File.Exists(filePath))
        {
            var bytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(bytes);
        }
        else
        {
            return null;
        }
    }
}
