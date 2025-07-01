using BotSharp.Abstraction.Messaging.Models.RichContent.Template;

namespace BotSharp.Plugin.ChartHandler.Functions;

public class GenerateChartFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GenerateChartFn> _logger;

    public string Name => "util-chart-generate_chart";
    public string Indication => "Generating chart";


    public GenerateChartFn(
        IServiceProvider services,
        ILogger<GenerateChartFn> logger)
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
        var inst = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, "util-chart-plot-instruction");
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

        message.Content = response;
        message.RichContent = new RichContent<IRichMessage>
        {
            Recipient = new Recipient { Id = convService.ConversationId },
            Message = new JsCodeTemplateMessage
            {
                Text = response
            }
        };
        message.StopCompletion = true;
        return true;
    }

    private async Task<string> GetChatCompletion(Agent agent, List<RoleDialogModel> dialogs)
    {
        try
        {
            var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
            var completion = CompletionProvider.GetChatCompletion(_services, provider: "openai", model: "gpt-4.1");
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
}
