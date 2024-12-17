using BotSharp.Plugin.Planner.TwoStaging;
using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class SummaryPlanFn : IFunctionCallback
{
    public string Name => "plan_summary";
    public string Indication => "Organizing and summarizing the final output results.";

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
        var states = _services.GetRequiredService<IConversationStateService>();

        states.SetState("max_tokens", "4096");
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var taskRequirement = states.GetState("requirement_detail");

        // Get table names
        var steps = states.GetState("planning_result").JsonArrayContent<SecondStagePlan>();
        var allTables = new List<string>();
        var ddlStatements = string.Empty;
        var domainKnowledge = states.GetState("planning_result");
        domainKnowledge += "\r\n" + states.GetState("domain_knowledges");
        var dictionaryItems = states.GetState("dictionary_items");
        var excelImportResult = states.GetState("excel_import_result");

        foreach (var step in steps)
        {
            allTables.AddRange(step.Tables);
        }
        var distinctTables = allTables.Distinct().ToList();

        var msgCopy = RoleDialogModel.From(message);
        msgCopy.FunctionArgs = JsonSerializer.Serialize(new
        {
            tables = distinctTables,
        });
        await fn.InvokeFunction("sql_table_definition", msgCopy);
        ddlStatements += "\r\n" + msgCopy.Content;
        states.SetState("table_ddls", ddlStatements);

        // Summarize and generate query
        var prompt = await GetSummaryPlanPrompt(msgCopy, taskRequirement, domainKnowledge, dictionaryItems, ddlStatements, excelImportResult);
        _logger.LogInformation($"Summary plan prompt:\r\n{prompt}");

        var plannerAgent = new Agent
        {
            Id = PlannerAgentId.TwoStagePlanner,
            Name = Name,
            Instruction = prompt,
            LlmConfig = currentAgent.LlmConfig
        };

        var summary = await GetAiResponse(plannerAgent);
        message.Content = summary.Content;

        await HookEmitter.Emit<IPlanningHook>(_services, async hook =>
            await hook.OnPlanningCompleted(nameof(TwoStageTaskPlanner), message)
        );

        return true;
    }

    private async Task<string> GetSummaryPlanPrompt(RoleDialogModel message, string taskDescription, string domainKnowledge, string dictionaryItems, string ddlStatement, string excelImportResult)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(PlannerAgentId.TwoStagePlanner);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "two_stage.summarize")?.Content ?? string.Empty;

        var additionalRequirements = new List<string>();
        await HookEmitter.Emit<IPlanningHook>(_services, async x =>
        {
            var requirement = await x.GetSummaryAdditionalRequirements(nameof(TwoStageTaskPlanner), message);
            additionalRequirements.Add(requirement);
        });

        var globalKnowledges = new List<string>();
        foreach (var hook in knowledgeHooks)
        {
            var k = await hook.GetGlobalKnowledges(message);
            globalKnowledges.AddRange(k);
        }

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", taskDescription },
            { "summary_requirements", string.Join("\r\n", additionalRequirements) },
            { "global_knowledges", globalKnowledges },
            { "domain_knowledges", domainKnowledge },
            { "dictionary_items", dictionaryItems },
            { "table_structure", ddlStatement },
            { "excel_import_result", excelImportResult }
        });
    }
    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        // Append text
        wholeDialogs.Last().Content += "\n\nIf the table structure didn't mention auto incremental, the data field id needs to insert id manually and you need to use max(id).\nFor example, you should use SET @id = select max(id) from table;";
        wholeDialogs.Last().Content += "\n\nTry if you can generate a single query to fulfill the needs.";

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
