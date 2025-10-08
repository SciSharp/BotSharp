using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Functions;

public class PyProgrammerFn : IFunctionCallback
{
    public string Name => "util-code-python_programmer";
    public string Indication => "Coding";

    private readonly IServiceProvider _services;
    private readonly ILogger<PyProgrammerFn> _logger;
    private readonly PythonInterpreterSettings _settings;

    public PyProgrammerFn(
        IServiceProvider services,
        ILogger<PyProgrammerFn> logger,
        PythonInterpreterSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
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
            TemplateDict = new Dictionary<string, object>
            {
                { "python_version", _settings.PythonVersion ?? "3.11" },
                { "user_requirement", args?.UserRquirement ?? string.Empty }
            }
        };

        var dialogs = routingCtx.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = convService.GetDialogHistory();
        }

        var messageLimit = _settings.CodeGeneration?.MessageLimit > 0 ? _settings.CodeGeneration.MessageLimit.Value : 50;
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
            var (isSuccess, result) = InnerRunCode(ret.PythonCode);
            if (isSuccess)
            {
                message.Content = result;
                message.RichContent = new RichContent<IRichMessage>
                {
                    Recipient = new Recipient { Id = convService.ConversationId },
                    Message = new ProgramCodeTemplateMessage
                    {
                        Text = ret.PythonCode ?? string.Empty,
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
    private (bool, string) InnerRunCode(string codeScript)
    {
        using (Py.GIL())
        {
            // Import necessary Python modules
            dynamic sys = Py.Import("sys");
            dynamic io = Py.Import("io");

            try
            {
                // Redirect standard output/error to capture it
                dynamic stringIO = io.StringIO();
                sys.stdout = stringIO;
                sys.stderr = stringIO;

                // Set global items
                using var globals = new PyDict();
                if (codeScript?.Contains("__main__") == true)
                {
                    globals.SetItem("__name__", new PyString("__main__"));
                }

                // Execute Python script
                PythonEngine.Exec(codeScript, globals);

                // Get result
                var result = stringIO.getvalue()?.ToString() as string;
                return (true, result?.TrimEnd('\r', '\n') ?? string.Empty);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error when executing inner python code. {ex.Message}";
                _logger.LogError(ex, errorMsg);
                return (false, errorMsg);
            }
            finally
            {
                // Restore the original stdout/stderr/argv
                sys.stdout = sys.__stdout__;
                sys.stderr = sys.__stderr__;
                sys.argv = new PyList();
            }
        }
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
        var provider = "openai";
        var model = "gpt-5";

        var state = _services.GetRequiredService<IConversationStateService>();
        provider = state.GetState("py_intepreter_llm_provider")
                        .IfNullOrEmptyAs(_settings.CodeGeneration?.LlmProvider)
                        .IfNullOrEmptyAs(provider);
        model = state.GetState("py_intepreter_llm_model")
                     .IfNullOrEmptyAs(_settings.CodeGeneration?.LlmModel)
                     .IfNullOrEmptyAs(model);

        return (provider, model);
    }

    private AgentLlmConfig GetLlmConfig()
    {
        var maxOutputTokens = _settings?.CodeGeneration?.MaxOutputTokens ?? 8192;
        var reasoningEffortLevel = _settings?.CodeGeneration?.ReasoningEffortLevel ?? "minimal";

        var state = _services.GetRequiredService<IConversationStateService>();
        maxOutputTokens = int.TryParse(state.GetState("py_intepreter_max_output_tokens"), out var tokens) ? tokens : maxOutputTokens;
        reasoningEffortLevel = state.GetState("py_intepreter_reasoning_effort_level").IfNullOrEmptyAs(reasoningEffortLevel);

        return new AgentLlmConfig
        {
            MaxOutputTokens = maxOutputTokens,
            ReasoningEffortLevel = reasoningEffortLevel
        };
    }
}
