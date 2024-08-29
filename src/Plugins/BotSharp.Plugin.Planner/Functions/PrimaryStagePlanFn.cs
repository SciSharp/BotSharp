using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Templating;
using System.Threading.Tasks;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Planner.TwoStaging.Models;
using Microsoft.Extensions.Logging;
using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Plugin.Planner.Functions;

public class PrimaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_primary_stage";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private object aiAssistant;

    public PrimaryStagePlanFn(IServiceProvider services, ILogger<PrimaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        //debug
        var state = _services.GetRequiredService<IConversationStateService>();
        state.SetState("max_tokens", "4096");
        var task = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);
        
        //get knowledge from vectordb
        var fn = _services.GetRequiredService<IRoutingService>();

        var msg = new ExtractedKnowledge
        {
            Question = task.Question,
        };
        var retrievalMessage = new RoleDialogModel(AgentRole.User, task.Requirements)
        {
            FunctionArgs = JsonSerializer.Serialize(msg),
            KnowledgeConfidence = 0.1f,
            Content = ""
        };
        await fn.InvokeFunction("knowledge_retrieval", retrievalMessage);
        message.Content = retrievalMessage.Content;

        var agentService = _services.GetRequiredService<IAgentService>();
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);

        //send knowledge to AI to refine and summarize the primary planning
        var firstPlanningPrompt = await GetFirstStagePlanPrompt(task, message);
        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "planning_1st",
            Instruction = firstPlanningPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };
        var response = await GetAIResponse(plannerAgent);
        message.Content = response.Content; 

        /*await fn.InvokeFunction("plan_secondary_stage", message);
        var items = message.Content.JsonArrayContent<SecondStagePlan>();

        //get all the related tables
        List<string> allTables = new List<string>();
        foreach (var item in items)
        {
            allTables.AddRange(item.Tables);
        }
        message.Data = allTables.Distinct().ToList();

        //get table DDL and stores in content
        var msg2 = RoleDialogModel.From(message);
        await fn.InvokeFunction("get_table_definition", msg2);
        message.SecondaryContent = msg2.Content;

        //summarize and generate query
        var summaryPlanningPrompt = await GetPlanSummaryPrompt(task, message);
        _logger.LogInformation(summaryPlanningPrompt);
        plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "planner_summary",
            Instruction = summaryPlanningPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };
        var response_summary = await GetAIResponse(plannerAgent);
        _logger.LogInformation(response_summary.Content);

        message.Content = response_summary.Content;
        message.StopCompletion = true;*/
        return true;
    }
    private async Task<string> GetFirstStagePlanPrompt(PrimaryRequirementRequest task, RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.Planner);
        var render = _services.GetRequiredService<ITemplateRender>();
        var template = aiAssistant.Templates.First(x => x.Name == "two_stage.1st.plan").Content;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = [JsonDocument.Parse("{}")],
            Results = [""]
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "task_description", task.Requirements },
            { "relevant_knowledges", new[]{ message.Content } },
            { "response_format", responseFormat }
        });
    }
    private async Task<string> GetPlanSummaryPrompt(PrimaryRequirementRequest task, RoleDialogModel message)
    {
        // save to knowledge base
        var agentService = _services.GetRequiredService<IAgentService>();
        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.AIAssistant);
        var render = _services.GetRequiredService<ITemplateRender>();
        var template = aiAssistant.Templates.First(x => x.Name == "planner_prompt.two_stage.summarize").Content;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = [JsonDocument.Parse("{}")],
            Results = [""]
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "table_structure", message.SecondaryContent }, ////check
            { "task_description", task.Requirements},
            { "relevant_knowledges", message.Content },
            { "response_format", responseFormat }
        });
    }
    private async Task<RoleDialogModel> GetAIResponse(Agent plannerAgent)
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
