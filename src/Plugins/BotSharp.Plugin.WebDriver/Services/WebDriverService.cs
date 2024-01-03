using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.WebDriver.Services;

public class WebDriverService
{
    private readonly IServiceProvider _services;

    public WebDriverService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<HtmlElementContextOut> FindElement(Agent agent, string html, string elementName, string messageId)
    {
        var parserInstruction = agent.Templates.First(x => x.Name == "html_parser").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        var prompt = render.Render(parserInstruction, new Dictionary<string, object>
        {
            { "html_content",  html },
            { "element_name",  elementName }
        });

        var completer = CompletionProvider.GetCompletion(_services,
            agentConfig: agent.LlmConfig);

        if (completer is ITextCompletion textCompleter)
        {
            var result = await textCompleter.GetCompletion(prompt, agent.Id, messageId);
            return result.JsonContent<HtmlElementContextOut>();
        }
        else if (completer is IChatCompletion chatCompleter)
        {
            var dialogs = new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, prompt)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = messageId
                }
            };
            var result = chatCompleter.GetChatCompletions(new Agent
            {
                Id = agent.Id,
                Name = agent.Name,
                Instruction = "You're a HTML Parser."
            }, dialogs);
            return result.Content.JsonContent<HtmlElementContextOut>();
        }

        return null;
    }
}
