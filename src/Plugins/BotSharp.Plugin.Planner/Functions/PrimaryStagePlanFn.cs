using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Templating;
using BotSharp.Plugin.Planner.TwoStaging.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.Planner.Functions;

public class PrimaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_primary_stage";
    private readonly IServiceProvider _services;

    public PrimaryStagePlanFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var task = JsonSerializer.Deserialize<PrimaryRequirementRequest>(message.FunctionArgs);

        if (!task.HasKnowledgeReference)
        {
            message.Content = "Search knowledge base for the solution instructions";
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.AIAssistant);
        var template = aiAssistant.Templates.First(x => x.Name == "planner_prompt.two_stage.1st.plan").Content;
        var render = _services.GetRequiredService<ITemplateRender>();
        render.Render(template, new Dictionary<string, object>
        {
            { "relevant_knowledges", message.Content }
        });
        //message.Content = task.Requirements;
        //message.Content += "\r\n\r\n=====\r\nGet the first primary step, plan the secondary steps.";
        return true;
    }
}
