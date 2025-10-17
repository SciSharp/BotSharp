using BotSharp.Abstraction.Coding;
using BotSharp.Abstraction.Files.Options;
using BotSharp.Abstraction.Files.Proccessors;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Contexts;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Options;
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
        CodeInstructOptions? codeOptions = null,
        FileInstructOptions? fileOptions = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
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
        var codeResponse = await RunCode(agent, message, templateName, codeOptions);
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
        var result = string.Empty;

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

            result = await GetTextCompletion(textCompleter, agent, prompt, message.MessageId);
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

            IFileProcessor? fileProcessor = null;
            if (!files.IsNullOrEmpty() && fileOptions != null)
            {
                fileProcessor = _services.GetServices<IFileProcessor>()
                                         .FirstOrDefault(x => x.Provider.IsEqualTo(fileOptions.Processor));
            }

            if (fileProcessor != null)
            {
                var fileResponse = await fileProcessor.HandleFilesAsync(agent, prompt, files, new FileHandleOptions
                {
                    Provider = provider,
                    Model = model,
                    Instruction = instruction,
                    UserMessage = message.Content,
                    TemplateName = templateName,
                    InvokeFrom = $"{nameof(InstructService)}.{nameof(Execute)}",
                    Data = state.GetStates().ToDictionary(x => x.Key, x => (object)x.Value)
                });
                result = fileResponse.Result.IfNullOrEmptyAs(string.Empty);
            }
            else
            {
                result = await GetChatCompletion(chatCompleter, agent, instruction, prompt, message.MessageId, files);
            }
            response.Text = result;
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
    private async Task<InstructResult?> RunCode(
        Agent agent,
        RoleDialogModel message,
        string templateName,
        CodeInstructOptions? codeOptions)
    {
        InstructResult? response = null;

        if (agent == null)
        {
            return response;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var hooks = _services.GetHooks<IInstructHook>(agent.Id);

        var codeProvider = codeOptions?.Processor ?? "botsharp-py-interpreter";
        var codeProcessor = _services.GetServices<ICodeProcessor>()
                                       .FirstOrDefault(x => x.Provider.IsEqualTo(codeProvider));
        
        if (codeProcessor == null)
        {
#if DEBUG
            _logger.LogWarning($"No code processor found. (Agent: {agent.Id}, Code processor: {codeProvider})");
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
        var codeScript = await agentService.GetAgentCodeScript(agent.Id, scriptName, scriptType: AgentCodeScriptType.Src);
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
            await hook.BeforeCompletion(agent, message);
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
        var codeResponse = await codeProcessor.RunAsync(context.CodeScript, options: new()
        {
            ScriptName = scriptName,
            Arguments = context.Arguments
        });

        response = new InstructResult
        {
            MessageId = message.MessageId,
            Template = scriptName,
            Text = codeResponse?.Result ?? codeResponse?.ErrorMsg
        };

        if (context?.Arguments != null)
        {
            context.Arguments.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        }

        // After code execution
        foreach (var hook in hooks)
        {
            await hook.AfterCompletion(agent, response);
            await hook.AfterCodeExecution(agent, response);
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = agent.Id,
                Provider = codeProcessor.Provider,
                Model = string.Empty,
                TemplateName = scriptName,
                UserMessage = message.Content,
                SystemInstruction = context?.CodeScript,
                CompletionText = response.Text
            });
        }

        return response;
    }

    private async Task<string> GetTextCompletion(
        ITextCompletion textCompleter,
        Agent agent,
        string text,
        string messageId)
    {
        var result = await textCompleter.GetCompletion(text, agent.Id, messageId);
        return result;
    }

    private async Task<string> GetChatCompletion(
        IChatCompletion chatCompleter,
        Agent agent,
        string instruction,
        string text,
        string messageId,
        IEnumerable<InstructFileModel>? files = null)
    {
        var result = await chatCompleter.GetChatCompletions(new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = instruction
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId,
                Files = files?.Select(x => new BotSharpFile { FileUrl = x.FileUrl, FileData = x.FileData, ContentType = x.ContentType }).ToList() ?? []
            }
        });

        return result.Content;
    }
}
