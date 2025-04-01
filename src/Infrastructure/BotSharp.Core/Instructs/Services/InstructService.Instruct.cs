using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Templating;
using System.Collections;
using System.Reflection;

namespace BotSharp.Core.Instructs;

public partial class InstructService
{
    public async Task<T?> Instruct<T>(string text, InstructOptions? options = null) where T : class
    {
        var agent = await BuildInnerAgent(options);
        var response = await GetAiResponse(text, agent, options);

        if (string.IsNullOrWhiteSpace(response.Content)) return null;

        var type = typeof(T);
        T? result = null;

        try
        {
            var botsharpOptions = _services.GetRequiredService<BotSharpOptions>();

            if (IsStringType(type))
            {
                result = response.Content as T;
            }
            else if (IsListType(type))
            {
                var content = response.Content.JsonArrayContent();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    result = JsonSerializer.Deserialize<T>(content, botsharpOptions.JsonSerializerOptions);
                }
            }
            else
            {
                var content = response.Content.JsonContent();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    result = JsonSerializer.Deserialize<T>(content, botsharpOptions.JsonSerializerOptions);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting ai response, {ex.Message}\r\n{ex.InnerException}");
        }

        return result;
    }

    private async Task<Agent> BuildInnerAgent(InstructOptions? options)
    {
        Agent? agent = null;
        string? instruction = null;
        
        if (!string.IsNullOrWhiteSpace(options?.AgentId))
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            agent = await agentService.GetAgent(options.AgentId);
            
            if (!string.IsNullOrWhiteSpace(options?.TemplateName))
            {
                var template = agent?.Templates?.FirstOrDefault(x => x.Name == options.TemplateName)?.Content ?? string.Empty;
                instruction = BuildInstruction(template, options?.Data ?? []);
            }
        }

        return new Agent
        {
            Id = agent?.Id ?? Guid.Empty.ToString(),
            Name = agent?.Name ?? "Unknown",
            Instruction = instruction,
            LlmConfig = agent?.LlmConfig ?? new()
        };
    }

    private string BuildInstruction(string instruction, Dictionary<string, object> data)
    {
        var render = _services.GetRequiredService<ITemplateRender>();

        return render.Render(instruction, data ?? new Dictionary<string, object>());
    }

    private async Task<RoleDialogModel> GetAiResponse(string text, Agent agent, InstructOptions? options)
    {
        var dialogs = BuildDialogs(text, options);
        var provider = options?.Provider ?? agent?.LlmConfig?.Provider ?? "openai";
        var model = options?.Model ?? agent?.LlmConfig?.Model ?? "gpt-4o";
        var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);
        return await completion.GetChatCompletions(agent, dialogs);
    }

    private List<RoleDialogModel> BuildDialogs(string text, InstructOptions? options)
    {
        var messages = new List<RoleDialogModel>();

        if (!string.IsNullOrWhiteSpace(options?.ConversationId))
        {
            var conv = _services.GetRequiredService<IConversationService>();
            var dialogs = conv.GetDialogHistory();
            messages.AddRange(dialogs);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            messages.Add(new RoleDialogModel(AgentRole.User, text));
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
