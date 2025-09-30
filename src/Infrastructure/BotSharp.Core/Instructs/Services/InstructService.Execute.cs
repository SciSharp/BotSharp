using BotSharp.Abstraction.CodeInterpreter;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Models;

namespace BotSharp.Core.Instructs;

public partial class InstructService
{
    public async Task<InstructResult> Execute(
        string agentId,
        RoleDialogModel message,
        string? instruction = null,
        string? llmTemplateName = null,
        IEnumerable<InstructFileModel>? files = null,
        CodeInstructOptions? codeOptions = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        var response = new InstructResult
        {
            MessageId = message.MessageId,
            Template = codeOptions?.CodeTemplateName ?? llmTemplateName
        };


        if (agent == null)
        {
            response.Text = $"Agent (id: {agentId}) does not exist!";
            return response;
        }

        if (agent.Disabled)
        {
            var content = $"This agent ({agent.Name}) is disabled, please install the corresponding plugin ({agent.Plugin.Name}) to activate this agent.";
            response.Text = content;
            return response;
        }

        // Run code template
        if (!string.IsNullOrWhiteSpace(codeOptions?.CodeTemplateName))
        {
            var codeInterpreter = _services.GetServices<ICodeInterpretService>()
                                           .FirstOrDefault(x => x.Provider.IsEqualTo(codeOptions?.CodeInterpretProvider.IfNullOrEmptyAs("python-interpreter")));

            if (codeInterpreter == null)
            {
                var error = "No code interpreter found.";
                _logger.LogError(error);
                response.Text = error;
            }
            else
            {
                var state = _services.GetRequiredService<IConversationStateService>();
                var arguments = state.GetStates().Select(x => new KeyValue(x.Key, x.Value));
                var result = await codeInterpreter.RunCode("", arguments);
                response.Text = result?.Result?.ToString();
            }
            return response;
        }
        

        // Trigger before completion hooks
        var hooks = _services.GetHooks<IInstructHook>(agentId);
        foreach (var hook in hooks)
        {
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
        var prompt = string.IsNullOrEmpty(llmTemplateName) ?
            agentService.RenderInstruction(agent) :
            agentService.RenderTemplate(agent, llmTemplateName);

        var completer = CompletionProvider.GetCompletion(_services,
            agentConfig: agent.LlmConfig);

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
                    Files = files?.Select(x => new BotSharpFile { FileUrl = x.FileUrl, FileData = x.FileData, ContentType = x.ContentType }).ToList() ?? []
                }
            });
            response.Text = result.Content;
        }


        foreach (var hook in hooks)
        {
            await hook.AfterCompletion(agent, response);
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = agentId,
                Provider = provider,
                Model = model,
                TemplateName = llmTemplateName,
                UserMessage = prompt,
                SystemInstruction = instruction,
                CompletionText = response.Text
            });
        }

        return response;
    }
}
