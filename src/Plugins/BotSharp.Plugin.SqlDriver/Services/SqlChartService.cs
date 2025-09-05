using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Repositories;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.SqlDriver.LlmContext;

namespace BotSharp.Plugin.SqlDriver.Services;

public class SqlChartService : IBotSharpChartService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SqlChartService> _logger;
    private readonly BotSharpOptions _botSharpOptions;

    public SqlChartService(
        IServiceProvider services,
        ILogger<SqlChartService> logger,
        BotSharpOptions botSharpOptions)
    {
        _services = services;
        _logger = logger;
        _botSharpOptions = botSharpOptions;
    }

    public string Provider => "sql_driver";

    public async Task<ChartDataResult?> GetConversationChartData(string conversationId, string messageId, ChartDataOptions options)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(options?.TargetStateName))
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var states = db.GetConversationStates(conversationId);
            var value = states?.GetValueOrDefault(options?.TargetStateName)?.Values?.LastOrDefault()?.Data;

            // To do
            //return new ChartDataResult();
        }

        // Dummy data for testing
        var data = new
        {
            categories = new string[] { "A", "B", "C", "D", "E" },
            values = new int[] { 42, 67, 29, 85, 53 }
        };

        return new ChartDataResult { Data = data };
    }

    public async Task<ChartCodeResult?> GetConversationChartCode(string conversationId, string messageId, ChartCodeOptions options)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        var agentService = _services.GetRequiredService<IAgentService>();

        var agentId = options.AgentId.IfNullOrEmptyAs(BuiltInAgentId.UtilityAssistant);
        var templateName = options.TemplateName.IfNullOrEmptyAs("util-chart-plot_instruction");
        var inst = GetChartCodeInstruction(agentId, templateName);

        var agent = await agentService.GetAgent(agentId);
        agent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = inst,
            LlmConfig = new AgentLlmConfig
            {
                MaxOutputTokens = options.Llm?.MaxOutputTokens ?? 8192,
                ReasoningEffortLevel = options.Llm?.ReasoningEffortLevel
            },
            TemplateDict = BuildChartStates(options)
        };

        var dialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel
            {
                Role = AgentRole.User,
                MessageId = messageId,
                Content = options.Text.IfNullOrEmptyAs("Please follow the instruction to generate response.")
            }
        };
        var response = await GetChatCompletion(agent, dialogs, options);
        var obj = response.JsonContent<ChartLlmContextOut>();

        return new ChartCodeResult
        {
            Code = obj?.JsCode,
            Language = "javascript"
        };
    }


    private Dictionary<string, object> BuildChartStates(ChartCodeOptions options)
    {
        var states = new Dictionary<string, object>();

        if (!options.States.IsNullOrEmpty())
        {
            foreach (var item in options.States)
            {
                if (item.Value == null)
                {
                    continue;
                }
                states[item.Key] = item.Value;
            }
        }
        return states;
    }

    private string GetChartCodeInstruction(string agentId, string templateName)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var templateContent = db.GetAgentTemplate(agentId, templateName);
        return templateContent;
    }

    private async Task<string> GetChatCompletion(Agent agent, List<RoleDialogModel> dialogs, ChartCodeOptions options)
    {
        try
        {
            var (provider, model) = GetLlmProviderModel(options);
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);
            var response = await completion.GetChatCompletions(agent, dialogs);
            return response.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error when generating chart code. {ex.Message}";
            _logger.LogWarning(ex, error);
            return error;
        }
    }

    private (string, string) GetLlmProviderModel(ChartCodeOptions options)
    {
        var provider = "openai";
        var model = "gpt-5";

        if (options?.Llm != null)
        {
            provider = options.Llm.Provider.IfNullOrEmptyAs(provider);
            model = options.Llm.Model.IfNullOrEmptyAs(model);
        }

        return (provider, model);
    }
}
