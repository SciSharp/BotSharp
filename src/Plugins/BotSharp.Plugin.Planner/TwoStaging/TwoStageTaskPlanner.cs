using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Core.Routing.Planning;
using Microsoft.EntityFrameworkCore;

namespace BotSharp.Plugin.Planner.TwoStaging;

public partial class TwoStageTaskPlanner : IRoutingPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public int MaxLoopCount => 10;
    private bool _isTaskCompleted;

    private Queue<FirstStagePlan> _plan1st = new Queue<FirstStagePlan>();
    private Queue<SecondStagePlan> _plan2nd = new Queue<SecondStagePlan>();

    private List<string> _executionContext = new List<string>();

    public TwoStageTaskPlanner(IServiceProvider services, ILogger<TwoStageTaskPlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        // push agent to routing context
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.Push(BuiltInAgentId.Planner, "Make plan in TwoStage planner");
        return new FunctionCallFromLlm
        {
            AgentName = router.Name,
            Response = dialogs.Last().Content,
            Function = "route_to_agent"
        };

        
        /*FirstStagePlan[] items = await GetFirstStagePlanAsync(router, messageId, dialogs);

        foreach (var item in items)
        {
            _plan1st.Enqueue(item);
        };

        // Get Second Stage Plan
        if (_plan2nd.IsNullOrEmpty())
        {
            var plan1 = _plan1st.Dequeue();

            if (plan1.ContainMultipleSteps)
            {
                SecondStagePlan[] items = await GetSecondStagePlanAsync(router, messageId, plan1, dialogs);

                foreach (var item in items)
                {
                    _plan2nd.Enqueue(item);
                }
            }
            else
            {
                _plan2nd.Enqueue(new SecondStagePlan
                {
                    Description = plan1.Task,
                    Tables = plan1.Tables,
                    Parameters = plan1.Parameters,
                    Results = plan1.Results,
                });
            }
        }

        var plan2 = _plan2nd.Dequeue();

        var secondStagePrompt = GetSecondStageTaskPrompt(router, plan2);
        var inst = new FunctionCallFromLlm
        {
            AgentName = "SQL Driver",
            Response = secondStagePrompt,
            Function = "route_to_agent"
        };

        inst.HandleDialogsByPlanner = true;
        _isTaskCompleted = _plan1st.IsNullOrEmpty() && _plan2nd.IsNullOrEmpty();

        return inst;*/
    }

    public List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var question = inst.Response;
        if (_executionContext.Count > 0)
        {
            var content = GetContext();
            question = $"CONTEXT:\r\n{content}\r\n" + inst.Response;
        }
        else
        {
            question = $"CONTEXT:\r\n{question}";
        }

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

        // Keep execution context
        _executionContext.Add(taskAgentDialogs.Last().Content);

        return true;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();

        if (message.StopCompletion || _isTaskCompleted)
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

    public string GetContext()
    {
        var content = "";
        foreach (var c in _executionContext)
        {
            content += $"* {c}\r\n";
        }
        return content;
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

    private string GetFirstStageNextPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "first_stage.next").Content;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
        });
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "response_format",  responseFormat },
        });
    }

    private async Task<SecondStagePlan[]> GetSecondStagePlanAsync(Agent router, string messageId, FirstStagePlan plan1st, List<RoleDialogModel> dialogs)
    {
        var secondStagePrompt = GetSecondStagePlanPrompt(router, plan1st);
        var firstStageSystemPrompt = await GetFirstStagePlanPrompt(router);

        var plan = new SecondStagePlan[0];

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var model = llmProviderService.GetProviderModel("azure-openai", "gpt-4");

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: "azure-openai",
            model: model.Name);

        string text = string.Empty;

        var conversations = dialogs.Where(x => x.Role != AgentRole.Function).ToList();
        conversations.Add(new RoleDialogModel(AgentRole.User, secondStagePrompt)
        {
            CurrentAgentId = router.Id,
            MessageId = messageId,
        });

        try
        {
            var response = await completion.GetChatCompletions(new Agent
            {
                Id = router.Id,
                Name = nameof(TwoStageTaskPlanner),
                Instruction = firstStageSystemPrompt
            }, conversations);

            text = response.Content;
            plan = response.Content.JsonArrayContent<SecondStagePlan>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}: {text}");
        }

        return plan;
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

    private string GetSecondStagePlanPrompt(Agent router, FirstStagePlan plan)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.two_stage.2nd.plan").Content;
        var responseFormat = JsonSerializer.Serialize(new SecondStagePlan
        {
            Tool = "tool name if task solution provided",
            Parameters = new JsonDocument[] { JsonDocument.Parse("{}") },
            Results = new string[] { "" }
        });
        var context = GetContext();
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description",  plan.Task },
            { "response_format",  responseFormat }
        });
    }
}
