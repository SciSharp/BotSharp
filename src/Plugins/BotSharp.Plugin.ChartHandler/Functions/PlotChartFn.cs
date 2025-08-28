using BotSharp.Abstraction.Messaging.Models.RichContent.Template;

namespace BotSharp.Plugin.ChartHandler.Functions;

public class PlotChartFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PlotChartFn> _logger;

    public string Name => "util-chart-plot_chart";
    public string Indication => "Plotting chart";


    public PlotChartFn(
        IServiceProvider services,
        ILogger<PlotChartFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var convService = _services.GetRequiredService<IConversationService>();

        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        var agent = await agentService.GetAgent(message.CurrentAgentId);
        var inst = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, "util-chart-plot_instruction");
        var innerAgent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = inst,
            TemplateDict = new Dictionary<string, object>
            {
                { "plotting_requirement", args?.PlottingRequirement ?? string.Empty },
                { "chart_element_id", $"chart-{message.MessageId}" }
            }
        };

        var response = await GetChatCompletion(innerAgent, [
            new RoleDialogModel(AgentRole.User, "Please follow the instruction to generate the javascript code.")
            {
                CurrentAgentId = message.CurrentAgentId,
                MessageId = message.MessageId
            }
        ]);

        var obj = response.JsonContent<LlmContextOut>();
        message.Content = obj?.GreetingMessage ?? "Here is the chart you ask for:";
        message.RichContent = new RichContent<IRichMessage>
        {
            Recipient = new Recipient { Id = convService.ConversationId },
            Message = new ProgramCodeTemplateMessage
            {
                Text = obj?.JsCode ?? string.Empty,
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

    private (string, string) GetLlmProviderModel()
    {
        var provider = "openai";
        var model = "gpt-5";

        var state = _services.GetRequiredService<IConversationStateService>();
        var settings = _services.GetRequiredService<ChartHandlerSettings>();
        provider = state.GetState("chart_plot_llm_provider")
                        .IfNullOrEmptyAs(settings.ChartPlot?.LlmProvider)
                        .IfNullOrEmptyAs(provider);
        model = state.GetState("chart_plot_llm_model")
                     .IfNullOrEmptyAs(settings.ChartPlot?.LlmModel)
                     .IfNullOrEmptyAs(model);

        return (provider, model);
    }
}
