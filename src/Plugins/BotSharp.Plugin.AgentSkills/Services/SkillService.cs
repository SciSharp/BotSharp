using AgentSkillsDotNet;
using BotSharp.Plugin.AgentSkills.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills.Services;

/// <summary>
/// Service implementation for managing Agent Skills.
/// Encapsulates AgentSkillsDotNet library and provides unified skill access.
/// Implements requirements: FR-1.1, FR-1.2, FR-1.3, FR-2.1, FR-3.1, NFR-1.1, NFR-4.2
/// </summary>
public class SkillService : ISkillService
{
    private readonly AgentSkillsFactory _factory;
    private readonly IServiceProvider _serviceProvider;
    private AgentSkillsSettings _settings;
    private readonly ILogger<SkillService> _logger;
    private AgentSkillsDotNet.AgentSkills? _agentSkills;
    private IList<AITool>? _tools;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the SkillService class.
    /// Implements requirement: FR-1.1 (Skill Discovery and Loading)
    /// </summary>
    /// <param name="factory">The AgentSkillsFactory for creating skill instances.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public SkillService(
        AgentSkillsFactory factory,
        IServiceProvider serviceProvider,
        ILogger<SkillService> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Initialize skills on construction
        InitializeSkills();
    }

    /// <summary>
    /// Initializes skill loading from configured directories.
    /// Implements requirements: FR-1.1, FR-1.2, FR-1.3, NFR-1.1
    /// </summary>
    private void InitializeSkills()
    {
        lock (_lock)
        {
            try
            {
                _settings = _serviceProvider.GetRequiredService<AgentSkillsSettings>();

                _logger.LogInformation("Initializing Agent Skills...");

                // FR-1.2: Load project-level skills
                if (_settings.EnableProjectSkills)
                {
                    var projectSkillsDir = _settings.GetProjectSkillsDirectory();
                    _logger.LogInformation("Loading project skills from {Directory}", projectSkillsDir);

                    if (Directory.Exists(projectSkillsDir))
                    {
                        _agentSkills = _factory.GetAgentSkills(projectSkillsDir);
                        var skillCount = _agentSkills.GetInstructions().Split("<skill>").Length - 1;
                        _logger.LogInformation("Loaded {Count} project skills", skillCount);
                    }
                    else
                    {
                        // FR-1.3: Directory not found - log warning but continue
                        _logger.LogWarning("Project skills directory not found: {Directory}", projectSkillsDir);
                    }
                }

                // FR-1.2: Load user-level skills (if enabled)
                // Note: Currently AgentSkillsDotNet doesn't support merging multiple directories
                // If both are enabled, project skills take precedence
                if (_settings.EnableUserSkills && _agentSkills == null)
                {
                    var userSkillsDir = _settings.GetUserSkillsDirectory();
                    _logger.LogInformation("Loading user skills from {Directory}", userSkillsDir);

                    if (Directory.Exists(userSkillsDir))
                    {
                        _agentSkills = _factory.GetAgentSkills(userSkillsDir);
                        var skillCount = _agentSkills.GetInstructions().Split("<skill>").Length - 1;
                        _logger.LogInformation("Loaded {Count} user skills", skillCount);
                    }
                    else
                    {
                        // FR-1.3: Directory not found - log warning but continue
                        _logger.LogWarning("User skills directory not found: {Directory}", userSkillsDir);
                    }
                }

                // FR-3.1: Convert skills to tools
                if (_agentSkills != null)
                {
                    _logger.LogDebug("Generating tools from skills...");

                    // FR-3.2: Generate tools based on configuration
                    var options = new AgentSkillsAsToolsOptions
                    {
                        IncludeToolForFileContentRead = _settings.EnableReadFileTool
                    };

                    _tools = _agentSkills.GetAsTools(
                        AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools,
                        options
                    );

                    var skillCount = _agentSkills.GetInstructions().Split("<skill>").Length - 1;
                    _logger.LogInformation(
                        "Generated {ToolCount} tools from {SkillCount} skills",
                        _tools?.Count ?? 0,
                        skillCount
                    );
                }
                else
                {
                    // No skills loaded
                    _tools = new List<AITool>();
                    _logger.LogWarning("No skills loaded. Ensure at least one skill directory exists and is configured.");
                }

                _logger.LogInformation("Agent Skills initialization completed successfully");
            }
            catch (Exception ex)
            {
                // FR-1.3: Loading failure should not interrupt application startup
                _logger.LogError(ex, "Failed to initialize Agent Skills");
                _agentSkills = null;
                _tools = new List<AITool>();
            }
        }
    }

    /// <summary>
    /// Gets all loaded skills.
    /// Implements requirement: FR-1.1
    /// </summary>
    public AgentSkillsDotNet.AgentSkills GetAgentSkills()
    {
        if (_agentSkills == null)
        {
            throw new InvalidOperationException("Skills not loaded. Check logs for initialization errors.");
        }

        return _agentSkills;
    }

    /// <summary>
    /// Gets skill instructions text for injection into Agent prompts.
    /// Implements requirement: FR-2.1
    /// </summary>
    public string GetInstructions()
    {
        if (_agentSkills == null)
        {
            _logger.LogWarning("GetInstructions called but no skills are loaded");
            return string.Empty;
        }

        try
        {
            // FR-2.1: Use AgentSkillsDotNet to generate instructions
            var instructions = _agentSkills.GetInstructions();
            var skillCount = instructions.Split("<skill>").Length - 1;
            _logger.LogDebug("Generated instructions for {Count} skills", skillCount);
            return instructions ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate skill instructions");
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the list of skill tools.
    /// Implements requirement: FR-3.1
    /// </summary>
    public IList<AITool> GetTools()
    {
        if (_tools == null)
        {
            _logger.LogWarning("GetTools called but no tools are available");
            return new List<AITool>();
        }

        return _tools;
    }

    /// <summary>
    /// Reloads all skills from configured directories.
    /// Implements requirement: NFR-4.2
    /// </summary>
    public async System.Threading.Tasks.Task ReloadSkillsAsync()
    {
        _logger.LogInformation("Reloading Agent Skills...");

        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                InitializeSkills();
                _logger.LogInformation("Agent Skills reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload Agent Skills");
                throw;
            }
        });
    }

    /// <summary>
    /// Gets the count of loaded skills.
    /// Implements requirement: NFR-2.2
    /// </summary>
    public int GetSkillCount()
    {
        if (_agentSkills == null)
        {
            return 0;
        }

        try
        {
            var instructions = _agentSkills.GetInstructions();
            return instructions.Split("<skill>").Length - 1;
        }
        catch
        {
            return 0;
        }
    }
}
