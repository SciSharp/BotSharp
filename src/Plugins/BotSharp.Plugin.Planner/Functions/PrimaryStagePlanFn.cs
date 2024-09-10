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
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();

        state.SetState("max_tokens", "4096");
        var task = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);
        var collectionName = knowledgeSettings.Default.CollectionName ?? KnowledgeCollectionName.BotSharp;

        // Get knowledge from vectordb
        var knowledges = new List<string>();
        foreach (var question in task.Questions)
        {
            var list = await knowledgeService.SearchVectorKnowledge(question, collectionName, new VectorSearchOptions
            {
                Confidence = 0.2f
            });

            knowledges.Add(string.Join("\r\n\r\n=====\r\n", list.Select(x => x.ToQuestionAnswer())));
        }

        // Get first stage planning prompt
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var firstPlanningPrompt = await GetFirstStagePlanPrompt(task.Requirements, knowledges);
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

    private async Task<string> GetFirstStagePlanPrompt(string taskDescription, List<string> relevantKnowledges)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(BuiltInAgentId.Planner);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "two_stage.1st.plan")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = [ JsonDocument.Parse("{}") ],
            Results = [ string.Empty ]
        });

        // Get global knowledges
        var globalKnowledges = new List<string>();
        foreach (var hook in knowledgeHooks)
        {
            var k = await hook.GetGlobalKnowledges();
            globalKnowledges.AddRange(k);
        }

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", taskDescription },
            { "global_knowledges", globalKnowledges },
            { "relevant_knowledges", relevantKnowledges },
            { "response_format", responseFormat }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        // Append text
        wholeDialogs.Last().Content += "\n\nYou must analyze the table description to infer the table relations.";

        var completion = CompletionProvider.GetChatCompletion(_services, 
            provider: plannerAgent.LlmConfig.Provider, 
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
