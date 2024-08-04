using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Planning;

public partial class TwoStagePlanner
{
    private async Task<FirstStagePlan[]> GetFirstStagePlanAsync(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var firstStagePlanPrompt = await GetFirstStagePlanPrompt(router);

        var plan = new FirstStagePlan[0];

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var provider = router.LlmConfig.Provider ?? "openai";
        var model = llmProviderService.GetProviderModel(provider, router.LlmConfig.Model ?? "gpt-4o");

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: provider,
            model: model.Name);

        string text = string.Empty;

        try
        {
            var response = await completion.GetChatCompletions(new Agent
            {
                Id = router.Id,
                Name = nameof(TwoStagePlanner),
                Instruction = firstStagePlanPrompt
            }, dialogs);

            text = response.Content;
            plan = response.Content.JsonArrayContent<FirstStagePlan>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}: {text}");
        }

        return plan;
    }

    private async Task<string> GetFirstStagePlanPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.two_stage.1st.plan").Content;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = new JsonDocument[]{ JsonDocument.Parse("{}") },
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
        var template = router.Templates.First(x => x.Name == "planner_prompt.first_stage.next").Content;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
        });
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "response_format",  responseFormat },
        });
    }
}
