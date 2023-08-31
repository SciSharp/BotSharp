using BotSharp.Abstraction.Templating;
using System.IO;
using System.Reflection;

namespace BotSharp.Core.Templating;

public class ResponseTemplateService : IResponseTemplateService
{
    private readonly IServiceProvider _services;
    public ResponseTemplateService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<string> RenderFunctionResponse(string agentId, RoleDialogModel message)
    {
        // Find response template
        var agentService = _services.GetRequiredService<IAgentService>();
        var dir = Path.Combine(agentService.GetAgentDataDir(agentId), "responses");
        var responses = Directory.GetFiles(dir)
            .Where(f => f.Split(Path.DirectorySeparatorChar).Last().Split('.')[1] == message.FunctionName)
            .ToList();

        if (responses.Count == 0)
        {
            return string.Empty;
        }

        var randomIndex = new Random().Next(0, responses.Count);
        var template = File.ReadAllText(responses[randomIndex]);

        var render = _services.GetRequiredService<ITemplateRender>();

        // Convert args and execute data to dictionary
        var dict = new Dictionary<string, object>();
        ExtractArgs(JsonSerializer.Deserialize<JsonDocument>(message.FunctionArgs), dict);
        ExtractExecuteData(message.ExecutionData, dict);

        var text = render.Render(template, dict);

        return text;
    }

    public async Task<string> RenderIntentResponse(string agentId, RoleDialogModel message)
    {
        // Find response template
        var agentService = _services.GetRequiredService<IAgentService>();
        var dir = Path.Combine(agentService.GetAgentDataDir(agentId), "responses");
        if (!Directory.Exists(dir))
        {
            return string.Empty;
        }
        var responses = Directory.GetFiles(dir)
            .Where(f => f.Split(Path.DirectorySeparatorChar).Last().Split('.')[1] == message.IntentName)
            .ToList();

        if (responses.Count == 0)
        {
            return string.Empty;
        }

        var randomIndex = new Random().Next(0, responses.Count);
        var template = File.ReadAllText(responses[randomIndex]);

        var render = _services.GetRequiredService<ITemplateRender>();

        // Convert args and execute data to dictionary
        var dict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(message.FunctionArgs))
        {
            ExtractArgs(JsonSerializer.Deserialize<JsonDocument>(message.FunctionArgs), dict);
        }

        if (message.ExecutionData != null)
        {
            ExtractExecuteData(message.ExecutionData, dict);
        }

        var text = render.Render(template, dict);

        return text;
    }

    private void ExtractArgs(JsonDocument args, Dictionary<string, object> dict)
    {
        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    dict[property.Name] = property.Value.ToString();
                }
            }
        }
    }

    private void ExtractExecuteData(object data, Dictionary<string, object> dict)
    {
        foreach (PropertyInfo property in data.GetType().GetProperties())
        {
            var value = property.GetValue(data, null);
            if (value != null)
            {
                dict[property.Name] = value;
            }
        }
    }
}
