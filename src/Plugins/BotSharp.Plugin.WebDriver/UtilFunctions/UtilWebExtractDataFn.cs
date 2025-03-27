using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BotSharp.Plugin.WebDriver.UtilFunctions
{
    public class UtilWebExtractDataFn : IFunctionCallback
    {
        public string Name => "util-web-extract_data_from_page";
        public string Indication => "Util Web Extract Data from web page";
        private readonly IServiceProvider _services;
        public UtilWebExtractDataFn(
            IServiceProvider services)
        {
            _services = services;
        }
        public async Task<bool> Execute(RoleDialogModel message)
        {
            var convService = _services.GetRequiredService<IConversationService>();
            var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
            var agentService = _services.GetRequiredService<IAgentService>();
            var agent = await agentService.LoadAgent(message.CurrentAgentId);
            var _browser = _services.GetRequiredService<IWebBrowser>();
            var webDriverService = _services.GetRequiredService<WebDriverService>();

            var contextId = webDriverService.GetMessageContext(message);
            var browerActionParams = new BrowserActionParams(agent, args, contextId, message.MessageId);
            message.Content = await _browser.ExtractData(browerActionParams);
            
            var path = webDriverService.GetScreenshotFilePath(message.MessageId);

            message.Data = await _browser.ScreenshotAsync(new MessageInfo
            {
                AgentId = message.CurrentAgentId,
                ContextId = contextId,
                MessageId = message.MessageId
            }, path);

            return true;
        }
    }
}
