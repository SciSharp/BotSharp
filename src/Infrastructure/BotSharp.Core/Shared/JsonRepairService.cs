using BotSharp.Abstraction.Shared;
using BotSharp.Abstraction.Shared.Options;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Shared;

/// <summary>
/// Service for repairing malformed JSON using LLM.
/// </summary>
public class JsonRepairService : IJsonRepairService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<JsonRepairService> _logger;

    private const string DEFAULT_TEMPLATE_NAME = "json_repair";

    public JsonRepairService(
        IServiceProvider services,
        ILogger<JsonRepairService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<string> RepairAsync(string malformedJson, JsonRepairOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(malformedJson))
        {
            return string.Empty;
        }

        var json = malformedJson.CleanJsonStr();
        if (IsValidJson(json))
        {
            return json;
        }

        var repairedJson = await RepairByLLMAsync(json, options);
        if (IsValidJson(repairedJson))
        {
            return repairedJson;
        }

        // Try repairing again if still invalid
        repairedJson = await RepairByLLMAsync(json, options);

        return IsValidJson(repairedJson) ? repairedJson : json;
    }

    public async Task<T?> RepairAndDeserializeAsync<T>(string malformedJson, JsonRepairOptions? options = null)
    {        
        var json = await RepairAsync(malformedJson, options);
        return json.Json<T>();
    }


    #region Private methods
    private bool IsValidJson(string malformedJson)
    {
        if (string.IsNullOrWhiteSpace(malformedJson))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(malformedJson);
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Error when parse json {malformedJson}");
            return false;
        }
    }

    private async Task<string> RepairByLLMAsync(string malformedJson, JsonRepairOptions? options)
    {
        var agentId = options?.AgentId ?? BuiltInAgentId.AIAssistant;
        var templateName = options?.TemplateName ?? DEFAULT_TEMPLATE_NAME;

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);
        
        var template = agent?.Templates?.FirstOrDefault(x => x.Name == templateName)?.Content;
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogWarning($"Template '{templateName}' cannot be found in agent '{agent?.Name ?? agentId}'");
            return malformedJson;
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        var data = options?.Data ?? [];
        data["input"] = malformedJson;
        var prompt = render.Render(template, data);

        try
        {
            var completion = CompletionProvider.GetChatCompletion(_services,
                        provider: options?.Provider ?? agent?.LlmConfig?.Provider ?? "openai",
                        model: options?.Model ?? agent?.LlmConfig?.Model ?? "gpt-4o-mini");

            var innerAgent = new Agent
            {
                Id = Guid.Empty.ToString(),
                Name = "JsonRepair",
                Instruction = "You are a JSON repair expert."
            };

            var dialogs = new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, prompt)
                {
                    FunctionName = templateName
                }
            };

            var response = await completion.GetChatCompletions(innerAgent, dialogs);

            _logger.LogInformation($"JSON repair result: {response.Content}");
            return response.Content.CleanJsonStr();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair and deserialize JSON.");
            return malformedJson;
        }
    }
    #endregion
}