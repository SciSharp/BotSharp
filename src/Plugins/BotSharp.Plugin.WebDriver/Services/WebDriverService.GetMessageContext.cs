using BotSharp.Abstraction.Infrastructures.Enums;

namespace BotSharp.Plugin.WebDriver.Services
{
    public partial class WebDriverService
    {
        public string GetMessageContext(RoleDialogModel message)
        {
            var states = _services.GetService<IConversationStateService>();
            var convService = _services.GetRequiredService<IConversationService>();
            var webDriverTaskId = states.GetState(StateConst.WEB_DRIVER_TASK_ID, "");
            var contextId = message.CurrentAgentId;
            if (!string.IsNullOrWhiteSpace(webDriverTaskId))
            {
                contextId = webDriverTaskId;
            }
            return contextId;
        }
    }
}
