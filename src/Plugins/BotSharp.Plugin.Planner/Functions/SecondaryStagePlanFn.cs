using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class SecondaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_secondary_stage";
    public string Indication => "Further analyzing and breaking down user sub-needs.";
    private readonly IServiceProvider _services;
    private readonly ILogger<SecondaryStagePlanFn> _logger;

    public SecondaryStagePlanFn(IServiceProvider services, ILogger<SecondaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var states = _services.GetRequiredService<IConversationStateService>();

        var msgSecondary = RoleDialogModel.From(message);
        var collectionName = knowledgeSettings.Default.CollectionName;
        var planPrimary = states.GetState("planning_result");

        var taskSecondary = JsonSerializer.Deserialize<SecondaryBreakdownTask>(msgSecondary.FunctionArgs);
 
        // Search knowledgebase
        var knowledges = await knowledgeService.SearchVectorKnowledge(taskSecondary.SolutionQuestion, collectionName, new VectorSearchOptions
        {
            Confidence = 0.6f
        });

        var knowledgeResults = string.Join("\r\n\r\n=====\r\n", knowledges.Select(x => x.ToQuestionAnswer()));

        // Get second stage planning prompt
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var secondPlanningPrompt = await GetSecondStagePlanPrompt(taskSecondary.TaskDescription, planPrimary, knowledgeResults, message);
        _logger.LogInformation(secondPlanningPrompt);

        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "planning_2nd",
            Instruction = secondPlanningPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };

        var response = await GetAiResponse(plannerAgent);
        message.Content = response.Content;
        _logger.LogInformation(response.Content);

        states.SetState("planning_result", response.Content);
        return true;
    }

    private async Task<string> GetSecondStagePlanPrompt(string taskDescription, string planPrimary, string knowledgeResults, RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var agent = await agentService.GetAgent(message.CurrentAgentId);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "two_stage.2nd.plan")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new SecondStagePlan
        {
            Parameters = [ JsonDocument.Parse("{}") ],
            Results = [ string.Empty ]
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", taskDescription },
            { "primary_plan", planPrimary },
            { "additional_knowledge", knowledgeResults },
            { "response_format",  responseFormat }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
