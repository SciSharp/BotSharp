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
        string? templateName = null,
        IEnumerable<InstructFileModel>? files = null,
        CodeInstructOptions? codeOptions = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        var response = new InstructResult
        {
            MessageId = message.MessageId,
            Template = templateName
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
        var codeResponse = await GetCodeResponse(agent, message, templateName, codeOptions);
        if (codeResponse != null)
        {
            return codeResponse;
        }


        // Before completion hooks
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
        var prompt = string.IsNullOrEmpty(templateName) ?
            agentService.RenderInstruction(agent) :
            agentService.RenderTemplate(agent, templateName);

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

        // After completion hooks
        foreach (var hook in hooks)
        {
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

    /// <summary>
    /// Get code response
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="message"></param>
    /// <param name="templateName"></param>
    /// <param name="codeOptions"></param>
    /// <returns></returns>
    private async Task<InstructResult?> GetCodeResponse(Agent agent, RoleDialogModel message, string templateName, CodeInstructOptions? codeOptions)
    {
        InstructResult? response = null;

        if (agent == null)
        {
            return response;
        }


        var state = _services.GetRequiredService<IConversationStateService>();
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var hooks = _services.GetHooks<IInstructHook>(agent.Id);

        var codeProvider = codeOptions?.CodeInterpretProvider.IfNullOrEmptyAs("botsharp-py-interpreter");
        var codeInterpreter = _services.GetServices<ICodeInterpretService>()
                                       .FirstOrDefault(x => x.Provider.IsEqualTo(codeProvider));
        
        if (codeInterpreter == null)
        {
#if DEBUG
            _logger.LogWarning($"No code interpreter found. (Agent: {agent.Id}, Code interpreter: {codeProvider})");
#endif
            return response;
        }

        // Get code script name
        var scriptName = string.Empty;
        if (!string.IsNullOrEmpty(codeOptions?.CodeScriptName))
        {
            scriptName = codeOptions.CodeScriptName;
        }
        else if (!string.IsNullOrEmpty(templateName))
        {
            scriptName = $"{templateName}.py";
        }

        if (string.IsNullOrEmpty(scriptName))
        {
#if DEBUG
            _logger.LogWarning($"Empty code script name. (Agent: {agent.Id}, {scriptName})");
#endif
            return response;
        }

        // Get code script
        var codeScript = db.GetAgentCodeScript(agent.Id, scriptName, scriptType: AgentCodeScriptType.Src);
        if (string.IsNullOrWhiteSpace(codeScript))
        {
#if DEBUG
            _logger.LogWarning($"Empty code script. (Agent: {agent.Id}, {scriptName})");
#endif
            return response;
        }

        // Get code arguments
        var arguments = codeOptions?.Arguments ?? [];
        if (arguments.IsNullOrEmpty())
        {
            arguments = state.GetStates().Select(x => new KeyValue(x.Key, x.Value)).ToList();
        }

        var context = new CodeInstructContext
        {
            CodeScript = codeScript,
            Arguments = arguments
        };

        // Before code execution
        foreach (var hook in hooks)
        {
            await hook.BeforeCodeExecution(agent, message, context);

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

        // Run code script
        var result = await codeInterpreter.RunCode(context.CodeScript, options: new()
        {
            Arguments = context.Arguments
        });

        response = new InstructResult
        {
            MessageId = message.MessageId,
            Template = scriptName,
            Text = result?.Result?.ToString()
        };

        // After code execution
        foreach (var hook in hooks)
        {
            await hook.AfterCodeExecution(agent, response);
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = agent.Id,
                Provider = codeInterpreter.Provider,
                Model = string.Empty,
                TemplateName = scriptName,
                UserMessage = string.Empty,
                SystemInstruction = string.Empty,
                CompletionText = response.Text
            });
        }

        return response;
    }
}
