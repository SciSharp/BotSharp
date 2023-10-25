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

    public async Task<InstructResult> Execute(Agent agent, RoleDialogModel message)
    {
        // Trigger before completion hooks
        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agent.Id)
            {
                continue;
            }

            await hook.BeforeCompletion(message);

            // Interrupted by hook
            if (message.StopCompletion)
            {
                return new InstructResult
                {
                    Text = message.Content
                };
            }
        }

        var completer = CompletionProvider.GetTextCompletion(_services);
        var result = await completer.GetCompletion(agent.Instruction);
        var response = new InstructResult
        {
            Text = result
        };

        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agent.Id)
            {
                continue;
            }

            await hook.AfterCompletion(response);
        }

        return response;
    }
}
