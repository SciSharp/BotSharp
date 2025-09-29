using BotSharp.Abstraction.Routing;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Runtime;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Functions;

public class PyInterpretationFn : IFunctionCallback
{
    public string Name => "util-code-python_interpreter";
    public string Indication => "Executing python code";

    private readonly IServiceProvider _services;
    private readonly ILogger<PyInterpretationFn> _logger;
    private readonly PythonInterpreterSettings _settings;

    public PyInterpretationFn(
        IServiceProvider services,
        ILogger<PyInterpretationFn> logger,
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
                { "user_requirement", args?.UserRquirement ?? string.Empty }
            }
        };

        var dialogs = routingCtx.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = convService.GetDialogHistory();
        }

        dialogs.Add(new RoleDialogModel(AgentRole.User, "Please follow the instruction and chat context to generate valid python code.")
        {
            CurrentAgentId = message.CurrentAgentId,
            MessageId = message.MessageId
        });

        var response = await GetChatCompletion(innerAgent, dialogs);
        var ret = response.JsonContent<LlmContextOut>();

        using (Py.GIL())
        {
            // Import necessary Python modules
            dynamic sys = Py.Import("sys");
            dynamic io = Py.Import("io");

            // Redirect standard output to capture it
            dynamic stringIO = io.StringIO();
            sys.stdout = stringIO;

            // Execute a simple Python script
            using var locals = new PyDict();
            PythonEngine.Exec(ret.PythonCode, null, locals);

            // Console.WriteLine($"Result from Python: {result}");
            message.Content = stringIO.getvalue();

            // Restore the original stdout
            sys.stdout = sys.__stdout__;
        }

        return true;
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
            templateName = "util-code-python_generate_instruction";
            templateContent = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, templateName);
        }

        return templateContent;
    }

    private (string, string) GetLlmProviderModel()
    {
        var provider = "openai";
        var model = "gpt-5";

        var state = _services.GetRequiredService<IConversationStateService>();
        provider = state.GetState("py_intepreter_llm_provider")
                        //.IfNullOrEmptyAs(_settings.ChartPlot?.LlmProvider)
                        .IfNullOrEmptyAs(provider);
        model = state.GetState("py_intepreter_llm_model")
                     //.IfNullOrEmptyAs(_settings.ChartPlot?.LlmModel)
                     .IfNullOrEmptyAs(model);

        return (provider, model);
    }

    private AgentLlmConfig GetLlmConfig()
    {
        var maxOutputTokens = 8192;
        var reasoningEffortLevel = "minimal";

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
