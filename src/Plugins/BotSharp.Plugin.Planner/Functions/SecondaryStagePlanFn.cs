using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Planner.TwoStaging.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.Planner.Functions;

public class SecondaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_secondary_stage";

    private readonly IServiceProvider _services;
    private readonly ILogger<SecondaryStagePlanFn> _logger;

    public SecondaryStagePlanFn(IServiceProvider services, ILogger<SecondaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var fn = _services.GetRequiredService<IRoutingService>();

        var msgSecondary = RoleDialogModel.From(message);
        var taskPrimary = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);

        msgSecondary.FunctionArgs = JsonSerializer.Serialize(new SecondaryBreakdownTask
        {
            TaskDescription = taskPrimary.Requirements
        });

        var taskSecondary = JsonSerializer.Deserialize<SecondaryBreakdownTask>(msgSecondary.FunctionArgs);
        var items = msgSecondary.Content.JsonArrayContent<FirstStagePlan>();

        msgSecondary.KnowledgeConfidence = 0.5f;
        foreach (var item in items)
        {
            if (item.NeedAdditionalInformation)
            {
                msgSecondary.FunctionArgs = JsonSerializer.Serialize(new ExtractedKnowledge
                {
                    Question = item.Task
                });
                await fn.InvokeFunction("knowledge_retrieval", msgSecondary);
                message.Content += msgSecondary.Content;
            }
        }

        // load agent
        var agentService = _services.GetRequiredService<IAgentService>();
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);

        var secondPlanningPrompt = await GetSecondStagePlanPrompt(taskSecondary, message);
        _logger.LogInformation(secondPlanningPrompt);

        var plannerAgent = new Agent
        {
            Id = string.Empty,
            Name = "test",
            Instruction = secondPlanningPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };

        var response = await GetAiResponse(plannerAgent);
        message.Content = response.Content;
        _logger.LogInformation(response.Content);
        return true;
    }
    private async Task<string> GetSecondStagePlanPrompt(SecondaryBreakdownTask task, RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var planner = await agentService.GetAgent(message.CurrentAgentId);
        var template = planner.Templates.FirstOrDefault(x => x.Name == "two_stage.2nd.plan")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new SecondStagePlan
        {
            Tool = "tool name if task solution provided", 
            Parameters = new JsonDocument[] { JsonDocument.Parse("{}") },
            Results = new string[] { "" }
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", task.TaskDescription },
            { "primary_plan", new[]{ message.Content } },
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
