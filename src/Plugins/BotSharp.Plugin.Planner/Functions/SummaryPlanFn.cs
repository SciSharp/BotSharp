using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class SummaryPlanFn : IFunctionCallback
{
    public string Name => "plan_summary";

    private readonly IServiceProvider _services;
    private readonly ILogger<SummaryPlanFn> _logger;

    public SummaryPlanFn(
        IServiceProvider services,
        ILogger<SummaryPlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var fn = _services.GetRequiredService<IRoutingService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();

        state.SetState("max_tokens", "4096");
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var taskRequirement = state.GetState("requirement_detail");

        // Get table names
        var steps = message.Content.JsonArrayContent<SecondStagePlan>();
        var allTables = new List<string>();
        var ddlStatements = "";
        var relevantKnowledge = message.Content;
        foreach (var step in steps)
        {
            allTables.AddRange(step.Tables);
        }
        var distinctTables = allTables.Distinct().ToList();
        foreach (var table in distinctTables)
        {
            var msgCopy = RoleDialogModel.From(message);
            msgCopy.FunctionArgs = JsonSerializer.Serialize(new  
            { 
                table = table,
            });
            await fn.InvokeFunction("get_table_definition", msgCopy);
            ddlStatements += "\r\n" + msgCopy.Content;
        }

        // Summarize and generate query
        var summaryPlanPrompt = await GetSummaryPlanPrompt(taskRequirement, relevantKnowledge, ddlStatements);
        _logger.LogInformation($"Summary plan prompt:\r\n{summaryPlanPrompt}");

        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "Planner Summary",
            Instruction = summaryPlanPrompt,
            LlmConfig = currentAgent.LlmConfig
        };

        var summary = await GetAiResponse(plannerAgent);
        message.Content = summary.Content;
        message.StopCompletion = true;

        return true;
    }

    private async Task<string> GetSummaryPlanPrompt(string taskDescription, string relevantKnowledge, string ddlStatement)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var agent = await agentService.GetAgent(BuiltInAgentId.Planner);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "two_stage.summarize")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = [JsonDocument.Parse("{}")],
            Results = [""]
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "table_structure", ddlStatement },
            { "task_description", taskDescription },
            { "relevant_knowledges", relevantKnowledge },
            { "response_format", responseFormat }
        });
    }
    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        // Append text
        wholeDialogs.Last().Content += "\n\nIf the table structure didn't mention auto incremental, the data field id needs to insert id manually and you need to use max(id) instead of LAST_INSERT_ID function.\nFor example, you should use SET @id = select max(id) from table;";
        wholeDialogs.Last().Content += "\n\nTry if you can generate a single query to fulfill the needs";

        var completion = CompletionProvider.GetChatCompletion(_services, 
            provider: plannerAgent.LlmConfig.Provider, 
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
