using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Templating;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public string RenderInstruction(Agent agent, IDictionary<string, object>? renderData = null)
    {
        var render = _services.GetRequiredService<ITemplateRender>();
        var conv = _services.GetRequiredService<IConversationService>();

        // merge instructions
        var instructions = new List<string> { agent.Instruction ?? string.Empty };
        var secondaryInstructions = agent.SecondaryInstructions?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        instructions.AddRange(secondaryInstructions);

        // update states
        var renderDict = renderData != null
                        ? new Dictionary<string, object>(renderData)
                        : CollectRenderData(agent);
        renderDict[TemplateRenderConstant.RENDER_AGENT] = agent;

        var res = render.Render(string.Join("\r\n", instructions), renderDict);
        return res;
    }

    public bool RenderFunction(Agent agent, FunctionDef def, IDictionary<string, object>? renderData = null)
    {
        var renderDict = new Dictionary<string, object>(renderData ?? agent.TemplateDict ?? []);
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

        if (!isRender)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(def.VisibilityExpression))
        {
            isRender = RenderVisibility(def.VisibilityExpression, renderDict);
        }

        return isRender;
    }

    public FunctionParametersDef? RenderFunctionProperty(Agent agent, FunctionDef def, IDictionary<string, object>? renderData = null)
    {
        var parameterDef = def?.Parameters?.DeepClone(options: _options);
        var propertyDef = parameterDef?.Properties;
        if (propertyDef == null)
        {
            return null;
        }

        var renderDict = new Dictionary<string, object>(renderData ?? agent.TemplateDict ?? []);
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
                matched = RenderVisibility(expression, renderDict);
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

    public (string, IEnumerable<FunctionDef>) PrepareInstructionAndFunctions(Agent agent, IDictionary<string, object>? renderData = null, StringComparer ? comparer = null)
    {
        var text = string.Empty;
        if (!string.IsNullOrEmpty(agent.Instruction) || !agent.SecondaryInstructions.IsNullOrEmpty())
        {
            text = RenderInstruction(agent, renderData);
        }

        var functions = FilterFunctions(text, agent, comparer);
        return (text, functions);
    }

    public string RenderTemplate(Agent agent, string templateName, IDictionary<string, object>? renderData = null)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var template = agent.Templates.FirstOrDefault(x => x.Name == templateName)?.Content ?? string.Empty;

        // update states
        var renderDict = renderData != null
                        ? new Dictionary<string, object>(renderData)
                        : CollectRenderData(agent);
        renderDict[TemplateRenderConstant.RENDER_AGENT] = agent;

        // render liquid template
        var content = render.Render(template, renderDict);

        HookEmitter.Emit<IContentGeneratingHook>(_services, async hook => await hook.OnRenderingTemplate(agent, templateName, content),
            agent.Id).Wait();

        return content;
    }

    public bool RenderVisibility(string? visibilityExpression, IDictionary<string, object> dict)
    {
        if (string.IsNullOrWhiteSpace(visibilityExpression))
        {
            return true;
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        var copy = new Dictionary<string, object>(dict);
        var result = render.Render(visibilityExpression, new Dictionary<string, object>
        {
            { "states", copy }
        });

        return result.IsEqualTo("visible");
    }

    public IDictionary<string, object> CollectRenderData(Agent agent)
    {
        var state = _services.GetRequiredService<IConversationStateService>();

        var innerDict = new Dictionary<string, object>();
        var dict = new Dictionary<string, object>(agent.TemplateDict ?? []);
        foreach (var p in dict)
        {
            innerDict[p.Key] = p.Value;
        }

        var states = new Dictionary<string, string>(state.GetStates());
        foreach (var p in states)
        {
            innerDict[p.Key] = p.Value;
        }

        return innerDict;
    }

    #region Private methods
    private IEnumerable<FunctionDef> FilterFunctions(string instruction, Agent agent, StringComparer? comparer = null)
    {
        var functions = agent.Functions.Concat(agent.SecondaryFunctions ?? []);
        if (agent.FuncVisMode.IsEqualTo(AgentFuncVisMode.Auto) && !string.IsNullOrWhiteSpace(instruction))
        {
            functions = FilterFunctions(instruction, functions, comparer);
        }
        return functions;
    }

    private IEnumerable<FunctionDef> FilterFunctions(string instruction, IEnumerable<FunctionDef> functions, StringComparer? comparer = null)
    {
        comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
        var matches = Regex.Matches(instruction, @"\b[A-Za-z0-9_-]+\b");
        var words = new HashSet<string>(matches.Select(m => m.Value), comparer);
        return functions.Where(x => words.Contains(x.Name, comparer));
    }
    #endregion
}
