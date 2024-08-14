using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Templating;
using Newtonsoft.Json.Linq;

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
        var isRender = true;

        var channels = def.Channels;
        if (channels != null)
        {
            var state = _services.GetRequiredService<IConversationStateService>();
            var channel = state.GetState("channel");
            if (!string.IsNullOrWhiteSpace(channel))
            {
                isRender = isRender && channels.Contains(channel);
            }
        }

        if (!isRender) return false;

        if (!string.IsNullOrWhiteSpace(def.VisibilityExpression))
        {
            var render = _services.GetRequiredService<ITemplateRender>();
            var result = render.Render(def.VisibilityExpression, new Dictionary<string, object>
            {
                { "states", agent.TemplateDict }
            });
            isRender = isRender && result == "visible";
        }

        return isRender;
    }

    public FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def)
    {
        var parameterDef = def?.Parameters;
        var propertyDef = parameterDef?.Properties;
        if (propertyDef == null) return null;

        var visibleExpress = "visibility_expression";
        var root = propertyDef.RootElement;
        var iterator = root.EnumerateObject();
        var visibleProps = new List<string>();
        while (iterator.MoveNext())
        {
            var prop = iterator.Current;
            var name = prop.Name;
            var node = prop.Value;
            var matched = true;
            if (node.TryGetProperty(visibleExpress, out var element))
            {
                var expression = element.GetString();
                var render = _services.GetRequiredService<ITemplateRender>();
                var result = render.Render(expression, new Dictionary<string, object>
                {
                    { "states", agent.TemplateDict }
                });
                matched = result == "visible";
            }

            if (matched)
            {
                visibleProps.Add(name);
            }
        }

        var rootObject = JObject.Parse(root.GetRawText());
        var clonedRoot = rootObject.DeepClone() as JObject;
        var required = parameterDef?.Required ?? new List<string>();
        foreach (var property in rootObject.Properties())
        {
            if (visibleProps.Contains(property.Name))
            {
                var value = clonedRoot.GetValue(property.Name) as JObject;
                if (value != null && value.ContainsKey(visibleExpress))
                {
                    value.Remove(visibleExpress);
                }
            }
            else
            {
                clonedRoot.Remove(property.Name);
                required.Remove(property.Name);
            }
        }

        parameterDef.Properties = JsonSerializer.Deserialize<JsonDocument>(clonedRoot.ToString());
        parameterDef.Required = required;
        return parameterDef; ;
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
