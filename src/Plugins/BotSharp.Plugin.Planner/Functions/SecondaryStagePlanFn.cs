using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class SecondaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_secondary_stage";
    public string Indication => "Further analyzing and breaking down user sub-needs.";

    private readonly IServiceProvider _services;
    private readonly ILogger<SecondaryStagePlanFn> _logger;

    public SecondaryStagePlanFn(
        IServiceProvider services,
        ILogger<SecondaryStagePlanFn> logger)
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
        var planResult = states.GetState("planning_result");

        var taskSecondary = JsonSerializer.Deserialize<SecondaryBreakdownTask>(msgSecondary.FunctionArgs);

        // Search knowledgebase
        var hooks = _services.GetServices<IKnowledgeHook>();
        var knowledges = new List<string>();
        foreach (var hook in hooks)
        {
            var k = await hook.GetDomainKnowledges(message, taskSecondary.SolutionQuestion);
            knowledges.AddRange(k);
        }
        knowledges = knowledges.Distinct().ToList();
        var knowledgeResults = string.Join("\r\n\r\n=====\r\n", knowledges);

        var knowledgeState = states.GetState("domain_knowledges");
        knowledgeState += String.Join("\r\n", knowledges);
        states.SetState("domain_knowledges", knowledgeState);

        // Get second stage planning prompt
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var prompt = await GetSecondStagePlanPrompt(taskSecondary.TaskDescription, planResult, knowledgeResults, message);
        _logger.LogInformation(prompt);

        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "SecondStagePlanner",
            Instruction = prompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };

        var response = await GetAiResponse(plannerAgent);
        message.Content = response.Content;
        _logger.LogInformation(response.Content);

        states.SetState("planning_result", response.Content);
        return true;
    }

    private async Task<string> GetSecondStagePlanPrompt(string taskDescription, string planResult, string knowledgeResults, RoleDialogModel message)
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
            { "primary_plan", planResult },
            { "additional_knowledge", knowledgeResults },
            { "response_format",  responseFormat }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        wholeDialogs.Last().Content += "\r\nOutput in JSON format.";

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
