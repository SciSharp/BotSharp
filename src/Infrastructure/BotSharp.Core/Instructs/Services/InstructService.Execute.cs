using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Instructs;

public partial class InstructService
{
    public async Task<InstructResult> Execute(string agentId, RoleDialogModel message,
        string? templateName = null, string? instruction = null, IEnumerable<InstructFileModel>? files = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        if (agent.Disabled)
        {
            var content = $"This agent ({agent.Name}) is disabled, please install the corresponding plugin ({agent.Plugin.Name}) to activate this agent.";
            return new InstructResult
            {
                MessageId = message.MessageId,
                Text = content
            };
        }

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

        var provider = string.Empty;
        var model = string.Empty;

        // Render prompt
        var prompt = string.IsNullOrEmpty(templateName) ?
            agentService.RenderedInstruction(agent) :
            agentService.RenderedTemplate(agent, templateName);

        var completer = CompletionProvider.GetCompletion(_services,
            agentConfig: agent.LlmConfig);

        var response = new InstructResult
        {
            MessageId = message.MessageId
        };
        if (completer is ITextCompletion textCompleter)
        {
            instruction = null;
            provider = textCompleter.Provider;
            model = textCompleter.Model;

            var result = await textCompleter.GetCompletion(prompt, agentId, message.MessageId);
            response.Text = result;
        }
        else if (completer is IChatCompletion chatCompleter)
        {
            provider = chatCompleter.Provider;
            model = chatCompleter.Model;

            if (instruction == "#TEMPLATE#")
            {
                instruction = prompt;
                prompt = message.Content;
            }

            var result = await chatCompleter.GetChatCompletions(new Agent
            {
                Id = agentId,
                Name = agent.Name,
                Instruction = instruction
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, prompt)
                {
                    CurrentAgentId = agentId,
                    MessageId = message.MessageId,
                    Files = files?.Select(x => new BotSharpFile { FileUrl = x.FileUrl, FileData = x.FileData }).ToList() ?? []
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
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = agentId,
                Provider = provider,
                Model = model,
                TemplateName = templateName,
                UserMessage = prompt,
                SystemInstruction = instruction,
                CompletionText = response.Text
            });
        }

        return response;
    }
}
