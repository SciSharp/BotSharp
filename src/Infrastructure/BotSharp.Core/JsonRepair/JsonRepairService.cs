using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Utilities;
using System.Text.Json;

namespace BotSharp.Core.JsonRepair;

/// <summary>
/// Service for repairing malformed JSON using LLM.
/// </summary>
public class JsonRepairService : IJsonRepairService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<JsonRepairService> _logger;

    private const string ROUTER_AGENT_ID = "01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a";
    private const string TEMPLATE_NAME = "json_repair";

    public JsonRepairService(
        IServiceProvider services,
        ILogger<JsonRepairService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<T?> RepairAndDeserialize<T>(string malformedJson)
    {
        
        try
        {
            // First try direct deserialization
            return malformedJson.JsonContent<T>();
        }
        catch
        {
            // Continue to repair
        }

        
        try
        {
            var repairedJson = await Repair(malformedJson);
            return  repairedJson.JsonContent<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair and deserialize JSON");
            return default;
        }
    }

    public async Task<string> Repair(string malformedJson)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var router = await agentService.GetAgent(ROUTER_AGENT_ID);
        
        var template = router.Templates?.FirstOrDefault(x => x.Name == TEMPLATE_NAME)?.Content;
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogWarning($"Template '{TEMPLATE_NAME}' not found in agent '{ROUTER_AGENT_ID}'");
            return malformedJson;
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        var prompt = render.Render(template, new Dictionary<string, object>
        {
            { "input", malformedJson }
        });

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        var agent = new Agent
        {
            Id = Guid.Empty.ToString(),
            Name = "JsonRepair",
            Instruction = "You are a JSON repair expert."
        };

        var dialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, prompt)
            {
                FunctionName = TEMPLATE_NAME
            }
        };

        var response = await completion.GetChatCompletions(agent, dialogs);

        _logger.LogInformation($"JSON repair result: {response.Content}");
        return response.Content;
    }
}

