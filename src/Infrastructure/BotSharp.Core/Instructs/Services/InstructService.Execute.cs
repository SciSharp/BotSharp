using BotSharp.Abstraction.CodeInterpreter;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Models;

namespace BotSharp.Core.Instructs;

public partial class InstructService
{
    private const string DEFAULT_CODE_INTERPRETER = "python-interpreter";

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
        var (text, isCodeComplete) = await GetCodeResponse(agentId, templateName, codeOptions);
        if (isCodeComplete)
        {
            response.Text = text;
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

    private async Task<(string?, bool)> GetCodeResponse(string agentId, string templateName, CodeInstructOptions? codeOptions)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var isComplete = false;
        var response = string.Empty;

        var codeProvider = codeOptions?.CodeInterpretProvider.IfNullOrEmptyAs(DEFAULT_CODE_INTERPRETER);
        var codeInterpreter = _services.GetServices<ICodeInterpretService>()
                                       .FirstOrDefault(x => x.Provider.IsEqualTo(codeProvider));
        
        if (codeInterpreter == null)
        {
            _logger.LogWarning($"No code interpreter found. (Agent: {agentId}, Code interpreter: {codeProvider})");
            return (response, isComplete);
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
            _logger.LogWarning($"Empty code script name. (Agent: {agentId}, {scriptName})");
            return (response, isComplete);
        }

        // Get code script
        var codeScript = db.GetAgentCodeScript(agentId, scriptName);
        if (string.IsNullOrWhiteSpace(codeScript))
        {
            _logger.LogWarning($"Empty code script. (Agent: {agentId}, {scriptName})");
            return (response, isComplete);
        }

        // Get code arguments
        var arguments = codeOptions?.Arguments ?? [];
        if (arguments.IsNullOrEmpty())
        {
            arguments = state.GetStates().Select(x => new KeyValue(x.Key, x.Value)).ToList();
        }

        // Run code script
        var result = await codeInterpreter.RunCode(codeScript, options: new()
        {
            Arguments = arguments
        });

        response = result?.Result?.ToString();
        isComplete = true;
        return (response, isComplete);
    }
}
