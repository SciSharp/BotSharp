using BotSharp.Abstraction.Coding.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Functions;

public class PyProgrammerFn : IFunctionCallback
{
    public string Name => "util-code-python_programmer";
    public string Indication => "Coding";

    private readonly IServiceProvider _services;
    private readonly ILogger<PyProgrammerFn> _logger;
    private readonly CodingSettings _codingSettings;
    private readonly PythonInterpreterSettings _pySettings;

    public PyProgrammerFn(
        IServiceProvider services,
        ILogger<PyProgrammerFn> logger,
        CodingSettings codingSettings,
        PythonInterpreterSettings pySettings)
    {
        _services = services;
        _logger = logger;
        _codingSettings = codingSettings;
        _pySettings = pySettings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var convService = _services.GetRequiredService<IConversationService>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();

        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);

        var agent = await agentService.GetAgent(message.CurrentAgentId);
        var inst = GetPyCodeInterpreterInstruction(message.CurrentAgentId);
        var innerAgent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = inst,
            LlmConfig = GetLlmConfig(),
            TemplateDict = new()
            {
                ["python_version"] = _pySettings.PythonVersion ?? "3.11",
                ["user_requirement"] = args?.UserRquirement ?? string.Empty
            }
        };

        var dialogs = routingCtx.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = convService.GetDialogHistory();
        }

        var messageLimit = _codingSettings.CodeGeneration?.MessageLimit > 0 ? _codingSettings.CodeGeneration.MessageLimit.Value : 50;
        dialogs = dialogs.TakeLast(messageLimit).ToList();
        dialogs.Add(new RoleDialogModel(AgentRole.User, "Please follow the instruction and chat context to generate valid python code.")
        {
            CurrentAgentId = message.CurrentAgentId,
            MessageId = message.MessageId
        });

        var response = await GetChatCompletion(innerAgent, dialogs);
        var ret = response.JsonContent<LlmContextOut>();

        try
        {
            var (isSuccess, result) = await InnerRunCode(ret.PythonCode);
            if (isSuccess)
            {
                message.Content = result;
                message.RichContent = new RichContent<IRichMessage>
                {
                    Recipient = new Recipient { Id = convService.ConversationId },
                    Message = new ProgramCodeTemplateMessage
                    {
                        Text = result,
                        CodeScript = ret.PythonCode,
                        Language = "python"
                    }
                };
                message.StopCompletion = true;
            }
            else
            {
                message.Content = result;
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error when executing python code. {ex.Message}";
            message.Content = errorMsg;
            _logger.LogError(ex, errorMsg);
        }

        return true;
    }

    /// <summary>
    /// Run python code script => (isSuccess, result)
    /// </summary>
    /// <param name="codeScript"></param>
    /// <returns></returns>
    private async Task<(bool, string)> InnerRunCode(string codeScript)
    {
        var codeProvider = _codingSettings.CodeExecution?.Processor;
        codeProvider = !string.IsNullOrEmpty(codeProvider) ? codeProvider : BuiltInCodeProcessor.PyInterpreter;
        var processor = _services.GetServices<ICodeProcessor>()
                                 .FirstOrDefault(x => x.Provider.IsEqualTo(codeProvider));

        if (processor == null)
        {
            return (false, "Unable to execute python code script.");
        }

        var (useLock, useProcess, timeoutSeconds) = GetCodeExecutionConfig();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        var response = await processor.RunAsync(codeScript, options: new()
        {
            UseLock = useLock,
            UseProcess = useProcess
        }, cancellationToken: cts.Token);

        if (response == null || !response.Success)
        {
            return (false, !string.IsNullOrEmpty(response?.ErrorMsg) ? response.ErrorMsg : "Failed to execute python code script.");
        }

        return (true, response.Result);
    }

    private async Task<string> GetChatCompletion(Agent agent, List<RoleDialogModel> dialogs)
    {
        try
        {
            var (provider, model) = GetLlmProviderModel();
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);
            var response = await completion.GetChatCompletions(agent, dialogs);
            return response.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error when generating python code. {ex.Message}";
            _logger.LogWarning(ex, error);
            return error;
        }
    }

    private string GetPyCodeInterpreterInstruction(string agentId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var templateContent = string.Empty;
        var templateName = state.GetState("python_generate_template");

        if (!string.IsNullOrEmpty(templateName))
        {
            templateContent = db.GetAgentTemplate(agentId, templateName);
        }
        else
        {
            templateName = "py-code_generate_instruction";
            templateContent = db.GetAgentTemplate(BuiltInAgentId.AIProgrammer, templateName);
        }

        return templateContent;
    }

    private (string, string) GetLlmProviderModel()
    {
        var provider = _codingSettings.CodeGeneration?.Provider;
        var model = _codingSettings.CodeGeneration?.Model;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-5";

        return (provider, model);
    }

    private AgentLlmConfig GetLlmConfig()
    {
        var maxOutputTokens = _codingSettings?.CodeGeneration?.MaxOutputTokens ?? 8192;
        var reasoningEffortLevel = _codingSettings?.CodeGeneration?.ReasoningEffortLevel ?? "minimal";

        return new AgentLlmConfig
        {
            MaxOutputTokens = maxOutputTokens,
            ReasoningEffortLevel = reasoningEffortLevel
        };
    }

    private (bool, bool, int) GetCodeExecutionConfig()
    {
        var codeExecution = _codingSettings.CodeExecution;
        var defaultTimeoutSeconds = 10;

        var useLock = codeExecution?.UseLock ?? false;
        var useProcess = codeExecution?.UseProcess ?? false;
        var timeoutSeconds = codeExecution?.TimeoutSeconds > 0 ? codeExecution.TimeoutSeconds : defaultTimeoutSeconds;

        return (useLock, useProcess, timeoutSeconds);
    }
}
