using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Users;
using BotSharp.Plugin.ChatHub.Helpers;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.ChartHandler.Functions;

public class PlotChartFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PlotChartFn> _logger;
    private readonly BotSharpOptions _options;

    public string Name => "util-chart-plot_chart";
    public string Indication => "Plotting chart";


    public PlotChartFn(
        IServiceProvider services,
        ILogger<PlotChartFn> logger,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var convService = _services.GetRequiredService<IConversationService>();

        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);

        var agent = await agentService.GetAgent(message.CurrentAgentId);
        var inst = GetChartPlotInstruction(message.CurrentAgentId);
        var innerAgent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = inst,
            LlmConfig = new AgentLlmConfig
            {
                MaxOutputTokens = 8192
            },
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

        // Send report summary after 1.5 seconds if exists
        if (!string.IsNullOrEmpty(obj?.ReportSummary))
        {
            _ = Task.Run(async () =>
            {
                var services = _services.CreateScope().ServiceProvider;
                await Task.Delay(1500);
                await SendDelayedMessage(services, obj.ReportSummary, convService.ConversationId, agent.Id, agent.Name);
            });
        }

        message.StopCompletion = true;
        return true;
    }

    private async Task SendDelayedMessage(IServiceProvider services, string text, string conversationId, string agentId, string agentName)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            var messageData = new ChatResponseDto
            {
                ConversationId = conversationId,
                MessageId = messageId,
                Text = text,
                Sender = new() { FirstName = agentName, LastName = "", Role = AgentRole.Assistant }
            };
            
            var dialogModel = new RoleDialogModel(AgentRole.Assistant, text)
            {
                MessageId = messageId,
                CurrentAgentId = agentId,
                CreatedAt = DateTime.UtcNow
            };

            var storage = services.GetService<IConversationStorage>();
            storage?.Append(conversationId, dialogModel);
            await SendEvent(services, ChatEvent.OnMessageReceivedFromAssistant, conversationId, messageData);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send delayed message");
        }
    }

    private async Task SendEvent<T>(IServiceProvider services, string @event, string conversationId, T data, [CallerMemberName] string callerName = "")
    {
        var user = services.GetService<IUserIdentity>();
        var json = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        await EventEmitter.SendChatEvent(services, _logger, @event, conversationId, user?.Id, json, nameof(PlotChartFn), callerName);
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
            templateName = "util-chart-plot_instruction";
            templateContent = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, templateName);
        }

        return templateContent;
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
