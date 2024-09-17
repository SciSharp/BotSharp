using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Core.Routing.Planning;

namespace BotSharp.Plugin.Planner.TwoStaging;

public partial class TwoStageTaskPlanner : IRoutingPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public int MaxLoopCount => 10;

    public TwoStageTaskPlanner(IServiceProvider services, ILogger<TwoStageTaskPlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var inst = new FunctionCallFromLlm();
        var nextStepPrompt = await GetNextStepPrompt(router);

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        // text completion
        dialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, nextStepPrompt)
            {
                FunctionName = nameof(TwoStageTaskPlanner),
                MessageId = messageId
            }
        };
        var response = await completion.GetChatCompletions(router, dialogs);

        inst = response.Content.JsonContent<FunctionCallFromLlm>();

        // Fix LLM malformed response
        PlannerHelper.FixMalformedResponse(_services, inst);

        return inst;
    }

    public List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var question = inst.Response;

        var taskAgentDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, question)
            {
                MessageId = message.MessageId,
            }
        };

        return taskAgentDialogs;
    }

    public bool AfterHandleContext(List<RoleDialogModel> dialogs, List<RoleDialogModel> taskAgentDialogs)
    {
        dialogs.AddRange(taskAgentDialogs.Skip(1));

        return true;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        // Set user content as Planner's question
        message.FunctionName = inst.Function;
        message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);
        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();

        if (message.StopCompletion)
        {
            context.Empty(reason: $"Agent queue is cleared by {nameof(TwoStageTaskPlanner)}");
            return false;
        }

        if (dialogs.Last().Role == AgentRole.Assistant)
        {
            context.Empty();
            return false;
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.ResetRecursiveCounter();
        return true;
    }

    private async Task<string> GetFirstStagePlanPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "two_stage.1st.plan").Content;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = new JsonDocument[] { JsonDocument.Parse("{}") },
            Results = new string[] { "" }
        });

        var relevantKnowledges = new List<string>();
        var hooks = _services.GetServices<IKnowledgeHook>();
        foreach (var hook in hooks)
        {
            var k = await hook.GetRelevantKnowledges();
            relevantKnowledges.AddRange(k);
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "response_format",  responseFormat },
            { "relevant_knowledges", relevantKnowledges.ToArray() }
        });
    }

    private async Task<string> GetNextStepPrompt(Agent router)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var planner = await agentService.LoadAgent(BuiltInAgentId.Planner);
        var template = planner.Templates.First(x => x.Name == "two_stage.next").Content;
        var states = _services.GetRequiredService<IConversationStateService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { StateConst.EXPECTED_ACTION_AGENT,  states.GetState(StateConst.EXPECTED_ACTION_AGENT) },
            { StateConst.EXPECTED_GOAL_AGENT,  states.GetState(StateConst.EXPECTED_GOAL_AGENT) }
        });
    }

    private string GetSecondStageTaskPrompt(Agent router, SecondStagePlan plan)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.two_stage.2nd.task").Content;
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description",  plan.Description },
            { "related_tables",  plan.Tables },
            { "input_arguments", JsonSerializer.Serialize(plan.Parameters) },
            { "output_results", JsonSerializer.Serialize(plan.Results) },
        });
    }
}
