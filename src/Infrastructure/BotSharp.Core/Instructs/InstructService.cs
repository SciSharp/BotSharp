using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Instructs;

public partial class InstructService : IInstructService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public InstructService(IServiceProvider services, ILogger<InstructService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<InstructResult> Execute(string agentId, RoleDialogModel message, string? templateName = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        // Trigger before completion hooks
        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.BeforeCompletion(agent, message);

            // Interrupted by hook
            if (message.StopCompletion)
            {
                return new InstructResult
                {
                    MessageId = message.MessageId,
                    Text = message.Content
                };
            }
        }

        // Render prompt
        var prompt = string.IsNullOrEmpty(templateName) ?
            agentService.RenderedInstruction(agent) :
            agentService.RenderedTemplate(agent, templateName);

        var completer = CompletionProvider.GetCompletion(_services);
        var response = new InstructResult
        {
            MessageId = message.MessageId
        };
        if (completer is ITextCompletion textCompleter)
        {
            var result = await textCompleter.GetCompletion(prompt, agentId, message.MessageId);
            response.Text = result;
        }
        else if (completer is IChatCompletion chatCompleter)
        {
            var result = chatCompleter.GetChatCompletions(new Agent
            {
                Id = agentId,
                Name = agent.Name
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, prompt)
                {
                    CurrentAgentId = agentId,
                    MessageId = message.MessageId
                }
            });
            response.Text = result.Content;
        }


        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.AfterCompletion(agent, response);
        }

        return response;
    }
}
