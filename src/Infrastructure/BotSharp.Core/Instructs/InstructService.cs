using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
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
        // Trigger before completion hooks
        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.BeforeCompletion(message);

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
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);
        var prompt = string.IsNullOrEmpty(templateName) ? 
            agentService.RenderedInstruction(agent) :
            agentService.RenderedTemplate(agent, templateName);

        var completer = CompletionProvider.GetTextCompletion(_services);
        var result = await completer.GetCompletion(prompt, agentId, message.MessageId);
        var response = new InstructResult
        {
            MessageId = message.MessageId,
            Text = result
        };

        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.AfterCompletion(response);
        }

        return response;
    }
}
