using BotSharp.Abstraction.Agents.Enums;

namespace BotSharp.Plugin.WebDriver.Hooks;

public class WebDriverConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    public WebDriverConversationHook(IServiceProvider services)
    {
        _services = services;
    }

    public override async Task OnDialogRecordLoaded(RoleDialogModel dialog)
    {
        var webDriverService = _services.GetRequiredService<WebDriverService>();

        // load screenshot
        if (dialog.Role == AgentRole.Assistant)
        {
            dialog.Data = "data:image/png;base64," + webDriverService.GetScreenshotBase64(dialog.MessageId);
        }
        
        await base.OnDialogRecordLoaded(dialog);
    }
}
