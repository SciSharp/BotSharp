using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public string RenderedInstruction(Agent agent)
    {
        var render = _services.GetRequiredService<ITemplateRender>();
        // update states
        var conv = _services.GetRequiredService<IConversationService>();
        foreach (var t in conv.States.GetStates())
        {
            agent.TemplateDict[t.Key] = t.Value;
        }
        return render.Render(agent.Instruction, agent.TemplateDict);
    }

    public bool RenderFunction(Agent agent, FunctionDef def)
    {
        if (!string.IsNullOrEmpty(def.VisibilityExpression))
        {
            var render = _services.GetRequiredService<ITemplateRender>();
            var result = render.Render(def.VisibilityExpression, new Dictionary<string, object>
            {
                { "states", agent.TemplateDict }
            });
            return result == "visible";
        }

        return true;
    }

    public string RenderedTemplate(Agent agent, string templateName)
    {
        // render liquid template
        var render = _services.GetRequiredService<ITemplateRender>();
        var template = agent.Templates.First(x => x.Name == templateName).Content;
        // update states
        var conv = _services.GetRequiredService<IConversationService>();
        foreach (var t in conv.States.GetStates())
        {
            agent.TemplateDict[t.Key] = t.Value;
        }

        var content = render.Render(template, agent.TemplateDict);

        HookEmitter.Emit<IContentGeneratingHook>(_services, async hook =>
            await hook.OnRenderingTemplate(agent, templateName, content)
        ).Wait();

        return content;
    }
}
