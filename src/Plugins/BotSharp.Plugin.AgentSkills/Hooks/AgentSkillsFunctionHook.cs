using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.AgentSkills.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BotSharp.Plugin.AgentSkills.Hooks;

/// <summary>
/// Skill function registration hook
/// Implements requirement: FR-3.1
/// </summary>
public class AgentSkillsFunctionHook : AgentHookBase
{
    public override string SelfId => "471ca181-375f-b16f-7134-5f868ecd31c6";

    private readonly ISkillService _skillService;
    private readonly ILogger<AgentSkillsFunctionHook> _logger;

    /// <summary>
    /// Constructor
    /// Implements requirement: FR-3.1
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <param name="settings">Agent settings</param>
    /// <param name="skillService">Skill service</param>
    /// <param name="logger">Logger</param>
    public AgentSkillsFunctionHook(
        IServiceProvider services,
        AgentSettings settings,
        ISkillService skillService,
        ILogger<AgentSkillsFunctionHook> logger)
        : base(services, settings)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register skill tools when functions are loaded
    /// Implements requirement: FR-3.1
    /// </summary>
    /// <param name="functions">Function list</param>
    /// <returns>Whether to continue processing</returns>
    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        if (Agent.Skills.Any())
        {
            try
            {
                var skillNamejson = JsonSerializer.Serialize(new
                {
                    skillname = new
                    {
                        type = "string",
                        description = "skill name"
                    }
                });

         
                var filePathjson = JsonSerializer.Serialize(new
                {
                    filepath = new
                    {
                        type = "string",
                        description = "Skill-file path"
                    }
                });

                functions.Add(new FunctionDef
                {
                    Name = "get-skill-by-name",
                    Description = $"Get a specific skill by its name",
                    Parameters =
                    {
                        Properties = JsonSerializer.Deserialize<JsonDocument>(skillNamejson),
                        Required = new List<string>
                        {
                            "skillname"
                        }
                    }
                });
                functions.Add(new FunctionDef
                {
                    Name = "read-skill-file-content",
                    Description = $"Read the content of a Skill File by its path",
                    Parameters =
                    {
                        Properties = JsonSerializer.Deserialize<JsonDocument>(filePathjson),
                        Required = new List<string>
                        {
                            "filepath"
                        }
                    }
                });

                functions.Add(new FunctionDef
                {
                    Name = "GetInstructionsFn",
                    Description = $"Get a list of the available skills"
                }); 
            }
            catch (Exception ex)
            {
                // Tool registration failure should not interrupt Agent loading
                _logger.LogError(ex, "Failed to register skill tools");
            }
        }
        return base.OnFunctionsLoaded(functions);
    }

    /// <summary>
    /// Convert AIFunction's AdditionalProperties to FunctionParametersDef
    /// Implements requirement: FR-3.1
    /// </summary>
    /// <param name="jsonSchema">AIFunction's additional properties</param>
    /// <returns>Function parameter definition</returns>
    private FunctionParametersDef? ConvertToFunctionParametersDef(
        JsonElement jsonSchema)
    {
        try
        {
            var json = JsonSerializer.Serialize(jsonSchema);
            var doc = JsonDocument.Parse(json);

            JsonDocument? propertiesDoc = null;
            if (doc.RootElement.TryGetProperty("properties", out var propertiesElement))
            { 
                
                propertiesDoc = JsonDocument.Parse(propertiesElement.GetRawText());
            }

            return new FunctionParametersDef
            {
                Type = "object",
                Properties = propertiesDoc!,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert AdditionalProperties to FunctionParametersDef");
            return null;
        }
    }
}
