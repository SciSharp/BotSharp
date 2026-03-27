using AgentSkillsDotNet;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills.Tests.Services;

/// <summary>
/// Integration tests for SkillService class using real AgentSkillsDotNet library.
/// Tests requirements: NFR-2.3, FR-1.1, FR-1.2, FR-1.3, FR-2.1, FR-3.1, NFR-4.2
/// </summary>
public class SkillServiceTests
{
    private readonly AgentSkillsFactory _factory;
    private readonly ILogger<SkillService> _logger;
    private readonly string _testSkillsPath;

    public SkillServiceTests()
    {
        // Use real AgentSkillsFactory instead of mocking
        _factory = new AgentSkillsFactory();
        _logger = new TestLogger<SkillService>();
        
        // Use test skills directory
        _testSkillsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test-skills");
    }

    private AgentSkillsSettings CreateSettings(
        bool enableProjectSkills = true,
        bool enableUserSkills = false,
        string? projectSkillsDir = null)
    {
        return new AgentSkillsSettings
        {
            EnableProjectSkills = enableProjectSkills,
            EnableUserSkills = enableUserSkills,
            ProjectSkillsDir = projectSkillsDir ?? _testSkillsPath,
            EnableReadFileTool = true,
            EnableListDirectoryTool = true,
            MaxOutputSizeBytes = 51200
        };
    }

    /// <summary>
    /// Test 3.3.1: Verify skills load successfully from test directory.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDirectory_ShouldLoadSkills()
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        var service = new SkillService(_factory, settings, _logger);

        // Assert
        service.Should().NotBeNull();
        var skillCount = service.GetSkillCount();
        skillCount.Should().BeGreaterThan(0, "test skills directory should contain at least one skill");
    }

    /// <summary>
    /// Test 3.3.2: Verify GetInstructions() returns valid XML format.
    /// </summary>
    [Fact]
    public void GetInstructions_WithLoadedSkills_ShouldReturnValidXml()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);

        // Act
        var instructions = service.GetInstructions();

        // Assert
        instructions.Should().NotBeNullOrEmpty();
        instructions.Should().Contain("<available_skills>");
        instructions.Should().Contain("<skill>");
        instructions.Should().Contain("</skill>");
        instructions.Should().Contain("</available_skills>");
        instructions.Should().Contain("<name>");
        instructions.Should().Contain("<description>");
    }

    /// <summary>
    /// Test 3.3.3: Verify GetTools() returns tool list.
    /// </summary>
    [Fact]
    public void GetTools_WithLoadedSkills_ShouldReturnToolList()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);

        // Act
        var tools = service.GetTools();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().NotBeEmpty("AgentSkillsDotNet should generate tools for loaded skills");
        
        // Verify expected tools are present
        var toolNames = tools.Select(t => t is AIFunction f ? f.Name : null).Where(n => n != null).ToList();
        toolNames.Should().Contain("get-available-skills", "get-available-skills tool should be present");
        toolNames.Should().Contain("get-skill-by-name", "get-skill-by-name tool should be present");
    }

    /// <summary>
    /// Test 3.3.4: Verify GetSkillCount() returns correct count.
    /// </summary>
    [Fact]
    public void GetSkillCount_WithMultipleSkills_ShouldReturnCorrectCount()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);

        // Act
        var count = service.GetSkillCount();

        // Assert
        count.Should().BeGreaterThan(0, "test skills directory should contain at least one skill");
        
        // Verify count matches the number of skills in test directory
        // We have: valid-skill, minimal-skill, skill-with-scripts, large-content-skill
        count.Should().Be(4, "test skills directory contains 4 skills");
    }

    /// <summary>
    /// Test 3.3.5: Verify directory not found logs warning but doesn't throw.
    /// </summary>
    [Fact]
    public void Constructor_WithNonExistentDirectory_ShouldLogWarningAndNotThrow()
    {
        // Arrange
        var nonExistentSettings = CreateSettings(projectSkillsDir: "/non/existent/path");

        // Act
        var act = () => new SkillService(_factory, nonExistentSettings, _logger);

        // Assert
        act.Should().NotThrow("service should handle missing directory gracefully");
        
        // Verify service was created but no skills loaded
        var service = new SkillService(_factory, nonExistentSettings, _logger);
        service.GetSkillCount().Should().Be(0, "no skills should be loaded from non-existent directory");
    }

    /// <summary>
    /// Test 3.3.6: Verify EnableProjectSkills configuration is respected.
    /// </summary>
    [Fact]
    public void Constructor_WithProjectSkillsDisabled_ShouldNotLoadProjectSkills()
    {
        // Arrange
        var disabledSettings = CreateSettings(enableProjectSkills: false, enableUserSkills: false);

        // Act
        var service = new SkillService(_factory, disabledSettings, _logger);

        // Assert
        service.GetSkillCount().Should().Be(0, "no skills should be loaded when both are disabled");
        service.GetInstructions().Should().BeEmpty();
        service.GetTools().Should().BeEmpty();
    }

    /// <summary>
    /// Test 3.3.6: Verify EnableUserSkills configuration is respected.
    /// </summary>
    [Fact]
    public void Constructor_WithUserSkillsEnabled_ShouldLoadUserSkills()
    {
        // Arrange
        var userSettings = CreateSettings(enableProjectSkills: false, enableUserSkills: true);
        userSettings.UserSkillsDir = _testSkillsPath;

        // Act
        var service = new SkillService(_factory, userSettings, _logger);

        // Assert
        service.GetSkillCount().Should().BeGreaterThan(0, "user skills should be loaded");
    }

    /// <summary>
    /// Test 3.3.7: Verify ReloadSkillsAsync() reloads skills.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ReloadSkillsAsync_ShouldReloadSkills()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);
        var initialCount = service.GetSkillCount();

        // Act
        await service.ReloadSkillsAsync();

        // Assert
        var reloadedCount = service.GetSkillCount();
        reloadedCount.Should().Be(initialCount, "skill count should remain the same after reload");
    }

    /// <summary>
    /// Test 3.3.8: Verify thread safety with concurrent ReloadSkillsAsync calls.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ReloadSkillsAsync_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => service.ReloadSkillsAsync())
            .ToArray();

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // Assert - should not throw and should complete successfully
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
        
        // Verify service is still functional after concurrent reloads
        service.GetSkillCount().Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test 3.3.5: Verify GetAgentSkills() throws when skills not loaded.
    /// </summary>
    [Fact]
    public void GetAgentSkills_WhenSkillsNotLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var failSettings = CreateSettings(enableProjectSkills: false, enableUserSkills: false);
        var service = new SkillService(_factory, failSettings, _logger);

        // Act
        var act = () => service.GetAgentSkills();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Skills not loaded*");
    }

    /// <summary>
    /// Test 3.3.2: Verify GetInstructions() returns empty string when no skills loaded.
    /// </summary>
    [Fact]
    public void GetInstructions_WhenNoSkillsLoaded_ShouldReturnEmptyString()
    {
        // Arrange
        var failSettings = CreateSettings(enableProjectSkills: false, enableUserSkills: false);
        var service = new SkillService(_factory, failSettings, _logger);

        // Act
        var instructions = service.GetInstructions();

        // Assert
        instructions.Should().BeEmpty();
    }

    /// <summary>
    /// Test 3.3.3: Verify GetTools() returns empty list when no skills loaded.
    /// </summary>
    [Fact]
    public void GetTools_WhenNoSkillsLoaded_ShouldReturnEmptyList()
    {
        // Arrange
        var failSettings = CreateSettings(enableProjectSkills: false, enableUserSkills: false);
        var service = new SkillService(_factory, failSettings, _logger);

        // Act
        var tools = service.GetTools();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().BeEmpty();
    }

    /// <summary>
    /// Test 3.3.4: Verify GetSkillCount() returns 0 when no skills loaded.
    /// </summary>
    [Fact]
    public void GetSkillCount_WhenNoSkillsLoaded_ShouldReturnZero()
    {
        // Arrange
        var failSettings = CreateSettings(enableProjectSkills: false, enableUserSkills: false);
        var service = new SkillService(_factory, failSettings, _logger);

        // Act
        var count = service.GetSkillCount();

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Test 3.3.6: Verify tool generation respects configuration.
    /// </summary>
    [Fact]
    public void Constructor_ShouldGenerateToolsBasedOnConfiguration()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);

        // Act
        var tools = service.GetTools();

        // Assert
        tools.Should().NotBeEmpty();
        
        // Verify tools are generated based on configuration
        var toolNames = tools.Select(t => t is AIFunction f ? f.Name : null).Where(n => n != null).ToList();
        toolNames.Should().Contain("get-available-skills", "get-available-skills tool should be generated");
        toolNames.Should().Contain("get-skill-by-name", "get-skill-by-name tool should be generated");
        
        // When EnableReadFileTool is true, read-skill-file-content should be present
        if (settings.EnableReadFileTool)
        {
            toolNames.Should().Contain("read-skill-file-content", "read-skill-file-content tool should be generated when enabled");
        }
    }
}
