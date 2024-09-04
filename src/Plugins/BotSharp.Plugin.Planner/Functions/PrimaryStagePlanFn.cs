using BotSharp.Abstraction.Knowledges;
using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class PrimaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_primary_stage";

    private readonly IServiceProvider _services;
    private readonly ILogger<PrimaryStagePlanFn> _logger;

    public PrimaryStagePlanFn(IServiceProvider services, ILogger<PrimaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        // Debug
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var fn = _services.GetRequiredService<IRoutingService>();

        state.SetState("max_tokens", "4096");
        var task = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);
        
        // Get knowledge from vectordb        
        var collectionName = knowledgeSettings.Default.CollectionName ?? KnowledgeCollectionName.BotSharp; ;
        var knowledges = await knowledgeService.SearchVectorKnowledge(task.Requirements, collectionName, new VectorSearchOptions
        {
            Confidence = 0.1f
        });
        message.Content = string.Join("\r\n\r\n=====\r\n", knowledges.Select(x => x.ToQuestionAnswer()));

        // Send knowledge to AI to refine and summarize the primary planning
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var firstPlanningPrompt = await GetFirstStagePlanPrompt(task, message);
        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "planning_1st",
            Instruction = firstPlanningPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };
        var response = await GetAiResponse(plannerAgent);
        message.Content = response.Content; 

        return true;
    }

    private async Task<string> GetFirstStagePlanPrompt(PrimaryRequirementRequest task, RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.Planner);
        var template = aiAssistant.Templates.FirstOrDefault(x => x.Name == "two_stage.1st.plan")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = [JsonDocument.Parse("{}")],
            Results = [""]
        });

        var globalKnowledges = new List<string>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();
        foreach (var hook in knowledgeHooks)
        {
            var k = await hook.GetGlobalKnowledges();
            globalKnowledges.AddRange(k);
        }

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", task.Requirements },
            { "global_knowledges", globalKnowledges },
            { "relevant_knowledges", new[]{ message.Content } },
            { "response_format", responseFormat }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        //add "test" to wholeDialogs' last element
        if(plannerAgent.Name == "planner_summary")
        {
            //add "test" to wholeDialogs' last element in a new paragraph
            wholeDialogs.Last().Content += "\n\nIf the table structure didn't mention auto incremental, the data field id needs to insert id manually and you need to use max(id) instead of LAST_INSERT_ID function.\nFor example, you should use SET @id = select max(id) from table;";
            wholeDialogs.Last().Content += "\n\nTry if you can generate a single query to fulfill the needs";
        }

        if (plannerAgent.Name == "planning_1st")
        {
            //add "test" to wholeDialogs' last element in a new paragraph
            wholeDialogs.Last().Content += "\n\nYou must analyze the table description to infer the table relations.";
        }

        var completion = CompletionProvider.GetChatCompletion(_services, 
            provider: plannerAgent.LlmConfig.Provider, 
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
