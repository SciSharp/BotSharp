using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Planning;

public partial class TwoStagePlanner
{
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
                Name = nameof(TwoStagePlanner),
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
