using Azure;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Planner.TwoStaging.Models;
using NetTopologySuite.Index.HPRtree;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.Planner.Functions;

public class SecondaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_secondary_stage";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public SecondaryStagePlanFn(IServiceProvider services, ILogger<SecondaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var fn = _services.GetRequiredService<IRoutingService>();
        var msg_secondary = RoleDialogModel.From(message);
        var task_primary = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);
        msg_secondary.FunctionArgs = JsonSerializer.Serialize(new SecondaryBreakdownTask
        {
            TaskDescription = task_primary.Requirements
        });
        var task_secondary = JsonSerializer.Deserialize<SecondaryBreakdownTask>(msg_secondary.FunctionArgs);
        var items = msg_secondary.Content.JsonArrayContent<FirstStagePlan>();

        msg_secondary.KnowledgeConfidence = 0.5f;
        foreach (var item in items)
        {
            if (item.NeedAdditionalInformation)
            {
                msg_secondary.FunctionArgs = JsonSerializer.Serialize(new ExtractedKnowledge
                {
                    Question = item.Task
                });
                await fn.InvokeFunction("knowledge_retrieval", msg_secondary);
                message.Content += msg_secondary.Content;
            }
        }
        // load agent
        var agentService = _services.GetRequiredService<IAgentService>();
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);

        var secondPlanningPrompt = await GetSecondStagePlanPrompt(task_secondary, message);
        _logger.LogInformation(secondPlanningPrompt);

        var plannerAgent = new Agent
        {
            Id = "",
            Name = "test",
            Instruction = secondPlanningPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };

        var response = await GetAIResponse(plannerAgent);
        message.Content = response.Content;
        _logger.LogInformation(response.Content);
        return true;
    }
    private async Task<string> GetSecondStagePlanPrompt(SecondaryBreakdownTask task, RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var planner = await agentService.GetAgent(message.CurrentAgentId);
        var render = _services.GetRequiredService<ITemplateRender>();
        var template = planner.Templates.First(x => x.Name == "two_stage.2nd.plan").Content;
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
    private async Task<RoleDialogModel> GetAIResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: plannerAgent.LlmConfig.Provider,
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
