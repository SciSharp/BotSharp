using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class PrimaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_primary_stage";
    public string Indication => "Currently analyzing and breaking down user requirements.";

    private readonly IServiceProvider _services;
    private readonly ILogger<PrimaryStagePlanFn> _logger;

    public PrimaryStagePlanFn(
        IServiceProvider services,
        ILogger<PrimaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();

        state.SetState("max_tokens", "4096");
        var task = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);
        var searchQuestions = new List<string>(task.Questions);
        searchQuestions.AddRange(task.NormQuestions);
        searchQuestions = searchQuestions.Distinct().ToList();

        // Get knowledge from vectordb
        var hooks = _services.GetServices<IKnowledgeHook>();
        var knowledges = new List<string>();
        foreach (var question in searchQuestions)
        {
            foreach (var hook in hooks)
            {
                var k = await hook.GetDomainKnowledges(message, question);
                knowledges.AddRange(k);
            }
        }
        knowledges = knowledges.Distinct().ToList();
        var knowledgeState = String.Join("\r\n", knowledges);
        state.SetState("domain_knowledges", knowledgeState);

        // Get first stage planning prompt
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var prompt = await GetFirstStagePlanPrompt(message, task.Requirements, knowledges);
        var plannerAgent = new Agent
        {
            Id = message.CurrentAgentId,
            Name = Name,
            Instruction = prompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };
        var response = await GetAiResponse(plannerAgent);
        message.Content = response.Content;

        var states = _services.GetRequiredService<IConversationStateService>();
        states.SetState("planning_result", response.Content);

        return true;
    }

    private async Task<string> GetFirstStagePlanPrompt(RoleDialogModel message, string taskDescription, List<string> domainKnowledges)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(PlannerAgentId.TwoStagePlanner);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "two_stage.1st.plan")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan{});

        // Get global knowledges
        var globalKnowledges = new List<string>();
        foreach (var hook in knowledgeHooks)
        {
            var k = await hook.GetGlobalKnowledges(message);
            globalKnowledges.AddRange(k);
        }

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", taskDescription },
            { "global_knowledges", globalKnowledges },
            { "domain_knowledges", domainKnowledges },
            { "response_format", responseFormat }
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
