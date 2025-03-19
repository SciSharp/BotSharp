using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Core.Instructs.Functions;

public class ExecuteTemplateFn : IFunctionCallback
{
    public string Name => "util-instruct-execute_template";

    private readonly IServiceProvider _services;
    private readonly ILogger<ExecuteTemplateFn> _logger;

    public ExecuteTemplateFn(
        IServiceProvider services,
        ILogger<ExecuteTemplateFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExecuteTemplateArgs>(message.FunctionArgs);
        if (string.IsNullOrEmpty(args.TemplateName))
        {
            message.Content = $"Invalid template name.";
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(message.CurrentAgentId);
        var template = agent.Templates.FirstOrDefault(x => x.Name.IsEqualTo(args.TemplateName));

        if (template == null)
        {
            message.Content = $"Cannot find template ({args.TemplateName}) in agent {agent.Name}";
            return false;
        }

        var response = await GetAiResponse(agent, args.TemplateName);
        message.Content = response;
        return true;
    }

    private async Task<string> GetAiResponse(Agent agent, string templateName)
    {
        try
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            var text = agentService.RenderedTemplate(agent, templateName);

            var completion = CompletionProvider.GetChatCompletion(_services, provider: agent.LlmConfig?.Provider, model: agent.LlmConfig?.Model);
            var response = await completion.GetChatCompletions(new Agent()
            {
                Id = agent.Id
            },
            new List<RoleDialogModel>
            {
                new(AgentRole.User, text)
            });

            var hooks = _services.GetServices<IInstructHook>();
            foreach (var hook in hooks)
            {
                if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agent.Id)
                {
                    continue;
                }

                await hook.OnResponseGenerated(new InstructResponseModel
                {
                    AgentId = agent.Id,
                    TemplateName = templateName,
                    Provider = completion.Provider,
                    Model = completion.Model,
                    UserMessage = text,
                    CompletionText = response.Content
                });
            }

            return response.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error when getting agent {agent.Name} instruction response.";
            _logger.LogWarning($"{error} {ex.Message}\r\n{ex.InnerException}");
            return error;
        }

    }
}
