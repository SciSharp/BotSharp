using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Plugin.AgentSkills.Services;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills.Hooks;

/// <summary>
/// Skill instruction injection hook
/// Implements requirements: FR-2.1, FR-2.2
/// </summary>
public class AgentSkillsInstructionHook : AgentHookBase
{
    public override string SelfId => "471ca181-375f-b16f-7134-5f868ecd31c6";

    private const string DefaultSkillsInstructionPrompt =
       """
        You have access to skills containing domain-specific knowledge and capabilities.
        Each skill provides specialized instructions, reference documents, and assets for specific tasks.
 
        {0}        

        When a task aligns with a skill's domain:
        1. Use `get-skill-by-name` to retrieve the skill's instructions
        2. Follow the provided guidance
        3. Use `read-skill-file-content` to read any references or other files mentioned by the skill

        Only load what is needed, when it is needed.
        """;

    private readonly ISkillService _skillService;
    private readonly ILogger<AgentSkillsInstructionHook> _logger;

    /// <summary>
    /// Constructor
    /// Implements requirement: FR-2.1
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <param name="settings">Agent settings</param>
    /// <param name="skillService">Skill service</param>
    /// <param name="logger">Logger</param>
    public AgentSkillsInstructionHook(
        IServiceProvider services,
        AgentSettings settings,
        ISkillService skillService,
        ILogger<AgentSkillsInstructionHook> logger)
        : base(services, settings)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Inject skill list when instruction is loaded
    /// Implements requirements: FR-2.1, FR-2.2
    /// </summary>
    /// <param name="template">Instruction template</param>
    /// <param name="dict">Instruction dictionary</param>
    /// <returns>Whether to continue processing</returns>
    public override bool OnInstructionLoaded(string template, IDictionary<string, object> dict)
    {
        // FR-2.2: Skip Routing and Planning type agents
        if (Agent.Type == AgentType.Routing || Agent.Type == AgentType.Planning)
        {
            _logger.LogDebug("Skipping skill injection for {AgentType} agent {AgentId}",
                Agent.Type, Agent.Id);
            return base.OnInstructionLoaded(template, dict);
        }

        try
        {
            // FR-2.1: Use GetInstructions() method provided by AgentSkillsDotNet
            var instructions = _skillService.GetInstructions(this.Agent);

            if (!string.IsNullOrEmpty(instructions))
            {
                var promptTemplate =  string.Format(DefaultSkillsInstructionPrompt, instructions);
                // Inject into instruction dictionary
                dict["SkillsInstructionPrompt"] = promptTemplate;

                _logger.LogInformation(
                    "Injected {Count} skills into agent {AgentId} instructions",
                    _skillService.GetSkillCount(),
                    Agent.Id);
            }
            else
            {
                _logger.LogWarning("No skills available to inject for agent {AgentId}", Agent.Id);
            }
        }
        catch (Exception ex)
        {
            // Injection failure should not interrupt agent loading
            _logger.LogError(ex, "Failed to inject skills into agent {AgentId}", Agent.Id);
        }

        return base.OnInstructionLoaded(template, dict);
    }
}
