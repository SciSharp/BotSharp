using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.WebDriver.Services;

public partial class WebDriverService
{
    public async Task<string> ExtraData(Agent agent, string html, string question, string messageId)
    {
        var parserInstruction = agent.Templates.First(x => x.Name == "extract_data").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        var prompt = render.Render(parserInstruction, new Dictionary<string, object>
        {
            { "content",  html },
            { "question",  question }
        });

        var completer = CompletionProvider.GetCompletion(_services,
            agentConfig: agent.LlmConfig);

        if (completer is ITextCompletion textCompleter)
        {
            var result = await textCompleter.GetCompletion(prompt, agent.Id, messageId);
            return result;
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
                Instruction = "You're a Content Extrator."
            }, dialogs);
            return result.Content;
        }

        return null;
    }
}
