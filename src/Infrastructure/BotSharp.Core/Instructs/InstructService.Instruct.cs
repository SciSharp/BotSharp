using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Templating;
using System.Collections;
using System.Reflection;

namespace BotSharp.Core.Instructs;

public partial class InstructService
{
    public async Task<T?> Instruct<T>(string instruction, string agentId, InstructOptions options) where T : class
    {
        var prompt = GetPrompt(instruction, options.Data);
        var response = await GetAiResponse(agentId, prompt, options);

        if (string.IsNullOrWhiteSpace(response.Content)) return null;

        var type = typeof(T);
        T? result = null;

        try
        {
            if (IsStringType(type))
            {
                result = response.Content as T;
            }
            else if (IsListType(type))
            {
                var text = response.Content.JsonArrayContent();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result = JsonSerializer.Deserialize<T>(text, _options.JsonSerializerOptions);
                }
            }
            else
            {
                var text = response.Content.JsonContent();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result = JsonSerializer.Deserialize<T>(text, _options.JsonSerializerOptions);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting ai response, {ex.Message}\r\n{ex.InnerException}");
        }

        return result;
    }

    private string GetPrompt(string instruction, Dictionary<string, object> data)
    {
        var render = _services.GetRequiredService<ITemplateRender>();

        return render.Render(instruction, data ?? new Dictionary<string, object>());
    }

    private async Task<RoleDialogModel> GetAiResponse(string agentId, string prompt, InstructOptions options)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var localAgent = new Agent
        {
            Id = agentId,
            Name = agent.Name,
            Instruction = prompt,
            TemplateDict = new()
        };

        var messages = BuildDialogs(options);
        var completion = CompletionProvider.GetChatCompletion(_services, provider: options.Provider, model: options.Model);

        return await completion.GetChatCompletions(localAgent, messages);
    }

    private List<RoleDialogModel> BuildDialogs(InstructOptions options)
    {
        var messages = new List<RoleDialogModel>();

        if (!string.IsNullOrWhiteSpace(options.ConversationId))
        {
            var conv = _services.GetRequiredService<IConversationService>();
            var dialogs = conv.GetDialogHistory();
            messages.AddRange(dialogs);
        }

        if (!string.IsNullOrWhiteSpace(options.Message))
        {
            messages.Add(new RoleDialogModel(AgentRole.User, options.Message));
        }

        return messages;
    }

    private bool IsStringType(Type? type)
    {
        if (type == null) return false;

        return type == typeof(string);
    }

    private bool IsListType(Type? type)
    {
        if (type == null) return false;

        var interfaces = type.GetTypeInfo().ImplementedInterfaces;
        return type.IsArray || interfaces.Any(x => x.Name == typeof(IEnumerable).Name);
    }
}
