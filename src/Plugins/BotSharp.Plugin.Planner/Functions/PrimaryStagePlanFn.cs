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

        // Get knowledge from vectordb
        var hooks = _services.GetServices<IKnowledgeHook>();
        var knowledges = new List<string>();
        foreach (var question in task.Questions)
        {
            foreach (var hook in hooks)
            {
                var k = await hook.GetRelevantKnowledges(message, question);
                knowledges.AddRange(k);
            }
        }
        knowledges = knowledges.Distinct().ToList();
        var knowledgeState = String.Join("\r\n", knowledges);
        state.SetState("relevant_knowledges", knowledgeState);

        // Get first stage planning prompt
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var prompt = await GetFirstStagePlanPrompt(message, task.Requirements, knowledges);
        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "FirstStagePlanner",
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

    private async Task<string> GetFirstStagePlanPrompt(RoleDialogModel message, string taskDescription, List<string> relevantKnowledges)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(BuiltInAgentId.Planner);
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
            { "relevant_knowledges", relevantKnowledges },
            { "response_format", responseFormat }
        });
    }

    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        // Append text
        wholeDialogs.Last().Content += "\n\nYou must analyze the table description to infer the table relations. Only output the JSON result.";

        var completion = CompletionProvider.GetChatCompletion(_services, 
            provider: plannerAgent.LlmConfig.Provider, 
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
