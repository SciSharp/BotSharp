using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Templating;
using Newtonsoft.Json.Linq;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public string RenderedInstruction(Agent agent)
    {
        var render = _services.GetRequiredService<ITemplateRender>();
        var conv = _services.GetRequiredService<IConversationService>();

        // merge instructions
        var instructions = new List<string> { agent.Instruction };
        var secondaryInstructions = agent.SecondaryInstructions?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        instructions.AddRange(secondaryInstructions);

        // update states
        var renderDict = new Dictionary<string, object>(agent.TemplateDict);
        foreach (var t in conv.States.GetStates())
        {
             renderDict[t.Key] = t.Value;
        }

        renderDict[TemplateRenderConstant.RENDER_AGENT] = agent;
        var res = render.Render(string.Join("\r\n", instructions), renderDict);
        return res;
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
            isRender = RenderVisibility(def.VisibilityExpression, agent.TemplateDict);
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
                matched = RenderVisibility(expression, agent.TemplateDict);
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
        return parameterDef;
    }

    public string RenderedTemplate(Agent agent, string templateName)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var template = agent.Templates.FirstOrDefault(x => x.Name == templateName)?.Content ?? string.Empty;

        // update states
        foreach (var t in conv.States.GetStates())
        {
            agent.TemplateDict[t.Key] = t.Value;
        }

        // render liquid template
        agent.TemplateDict[TemplateRenderConstant.RENDER_AGENT] = agent;
        var content = render.Render(template, agent.TemplateDict);

        HookEmitter.Emit<IContentGeneratingHook>(_services, async hook => await hook.OnRenderingTemplate(agent, templateName, content),
            agent.Id).Wait();

        return content;
    }

    public bool RenderVisibility(string? visibilityExpression, Dictionary<string, object> dict)
    {
        if (string.IsNullOrWhiteSpace(visibilityExpression))
        {
            return true;
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        var result = render.Render(visibilityExpression, new Dictionary<string, object>
        {
            { "states", dict ?? [] }
        });

        return result.IsEqualTo("visible");
    }
}
