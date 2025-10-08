using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Plugin.ChartHandler.Functions;

public class PlotChartFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PlotChartFn> _logger;
    private readonly ChartHandlerSettings _settings;

    public string Name => "util-chart-plot_chart";
    public string Indication => "Plotting chart";


    public PlotChartFn(
        IServiceProvider services,
        ILogger<PlotChartFn> logger,
        ChartHandlerSettings settings)
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
        var inst = GetChartPlotInstruction(message.CurrentAgentId);
        var innerAgent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = inst,
            LlmConfig = GetLlmConfig(),
            TemplateDict = new Dictionary<string, object>
            {
                { "plotting_requirement", args?.PlottingRequirement ?? string.Empty },
                { "chart_element_id", $"chart-{message.MessageId}" }
            }
        };

        var dialogs = routingCtx.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = convService.GetDialogHistory();
        }

        var messageLimit = _settings.ChartPlot?.MessageLimit > 0 ? _settings.ChartPlot.MessageLimit.Value : 50;
        dialogs = dialogs.TakeLast(messageLimit).ToList();
        dialogs.Add(new RoleDialogModel(AgentRole.User, "Please follow the instruction and chat context to generate valid javascript code.")
        {
            CurrentAgentId = message.CurrentAgentId,
            MessageId = message.MessageId
        });

        var response = await GetChatCompletion(innerAgent, dialogs);

        LlmContextOut? ret = null;
        var errorMsg = "Error when deserializing ai chart response";
        try
        {
            ret = response.JsonContent<LlmContextOut>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMsg);
            ret = new LlmContextOut
            {
                GreetingMessage = errorMsg
            };
        }
        
        message.Content = ret?.GreetingMessage ?? "Here is the chart you ask for:";
        message.RichContent = new RichContent<IRichMessage>
        {
            Recipient = new Recipient { Id = convService.ConversationId },
            Message = new ProgramCodeTemplateMessage
            {
                Text = ret?.JsCode ?? string.Empty,
                Language = "javascript"
            }
        };

        message.StopCompletion = true;
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
            var error = $"Error when plotting chart. {ex.Message}";
            _logger.LogWarning(ex, error);
            return error;
        }
    }

    private string GetChartPlotInstruction(string agentId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var templateContent = string.Empty;
        var templateName = state.GetState("chart_plot_template");

        if (!string.IsNullOrEmpty(templateName))
        {
            templateContent = db.GetAgentTemplate(agentId, templateName);
        }
        else
        {
            templateName = "chart-js-generate_instruction";
            templateContent = db.GetAgentTemplate(BuiltInAgentId.AIProgrammer, templateName);
        }

        return templateContent;
    }

    private (string, string) GetLlmProviderModel()
    {
        var provider = "openai";
        var model = "gpt-5";

        var state = _services.GetRequiredService<IConversationStateService>();
        provider = state.GetState("chart_plot_llm_provider")
                        .IfNullOrEmptyAs(_settings.ChartPlot?.LlmProvider)
                        .IfNullOrEmptyAs(provider);
        model = state.GetState("chart_plot_llm_model")
                     .IfNullOrEmptyAs(_settings.ChartPlot?.LlmModel)
                     .IfNullOrEmptyAs(model);

        return (provider, model);
    }

    private AgentLlmConfig GetLlmConfig()
    {
        var maxOutputTokens = _settings?.ChartPlot?.MaxOutputTokens ?? 8192;
        var reasoningEffortLevel = _settings?.ChartPlot?.ReasoningEffortLevel ?? "minimal";

        var state = _services.GetRequiredService<IConversationStateService>();
        maxOutputTokens = int.TryParse(state.GetState("chart_plot_max_output_tokens"), out var tokens) ? tokens : maxOutputTokens;
        reasoningEffortLevel = state.GetState("chart_plot_reasoning_effort_level").IfNullOrEmptyAs(reasoningEffortLevel);

        return new AgentLlmConfig
        {
            MaxOutputTokens = maxOutputTokens,
            ReasoningEffortLevel = reasoningEffortLevel
        };
    }
}
