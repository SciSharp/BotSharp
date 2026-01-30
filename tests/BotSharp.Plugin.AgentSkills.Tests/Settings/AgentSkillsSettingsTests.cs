using BotSharp.Plugin.AgentSkills.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.AgentSkills.Tests.Settings;

/// <summary>
/// Unit tests for AgentSkillsSettings configuration class.
/// Tests requirements: NFR-2.3, FR-6.1, FR-6.2
/// </summary>
public class AgentSkillsSettingsTests
{
    /// <summary>
    /// Test 2.2.1: Verify all default configuration values are set correctly.
    /// </summary>
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var settings = new AgentSkillsSettings();

        // Assert
        settings.EnableUserSkills.Should().BeTrue("user skills should be enabled by default");
        settings.EnableProjectSkills.Should().BeTrue("project skills should be enabled by default");
        settings.UserSkillsDir.Should().BeNull("user skills directory should be null by default");
        settings.ProjectSkillsDir.Should().BeNull("project skills directory should be null by default");
        settings.CacheSkills.Should().BeTrue("skill caching should be enabled by default");
        settings.ValidateOnStartup.Should().BeFalse("validation on startup should be disabled by default for performance");
        settings.SkillsCacheDurationSeconds.Should().Be(300, "cache duration should be 5 minutes by default");
        settings.EnableReadSkillTool.Should().BeTrue("read_skill tool should be enabled by default");
        settings.EnableReadFileTool.Should().BeTrue("read_skill_file tool should be enabled by default");
        settings.EnableListDirectoryTool.Should().BeTrue("list_skill_directory tool should be enabled by default");
        settings.MaxOutputSizeBytes.Should().Be(50 * 1024, "max output size should be 50KB by default");
    }

    /// <summary>
    /// Test 2.2.2: Verify configuration can be loaded from IConfiguration.
    /// </summary>
    [Fact]
    public void LoadFromConfiguration_ShouldBindCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AgentSkills:EnableUserSkills"] = "false",
            ["AgentSkills:EnableProjectSkills"] = "true",
            ["AgentSkills:UserSkillsDir"] = "/custom/user/skills",
            ["AgentSkills:ProjectSkillsDir"] = "/custom/project/skills",
            ["AgentSkills:CacheSkills"] = "false",
            ["AgentSkills:ValidateOnStartup"] = "true",
            ["AgentSkills:SkillsCacheDurationSeconds"] = "600",
            ["AgentSkills:EnableReadSkillTool"] = "false",
            ["AgentSkills:EnableReadFileTool"] = "true",
            ["AgentSkills:EnableListDirectoryTool"] = "false",
            ["AgentSkills:MaxOutputSizeBytes"] = "102400"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var settings = new AgentSkillsSettings();
        configuration.GetSection("AgentSkills").Bind(settings);

        // Assert
        settings.EnableUserSkills.Should().BeFalse();
        settings.EnableProjectSkills.Should().BeTrue();
        settings.UserSkillsDir.Should().Be("/custom/user/skills");
        settings.ProjectSkillsDir.Should().Be("/custom/project/skills");
        settings.CacheSkills.Should().BeFalse();
        settings.ValidateOnStartup.Should().BeTrue();
        settings.SkillsCacheDurationSeconds.Should().Be(600);
        settings.EnableReadSkillTool.Should().BeFalse();
        settings.EnableReadFileTool.Should().BeTrue();
        settings.EnableListDirectoryTool.Should().BeFalse();
        settings.MaxOutputSizeBytes.Should().Be(102400);
    }

    /// <summary>
    /// Test 2.2.3: Verify Validate() returns no errors for valid configuration.
    /// </summary>
    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnNoErrors()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            EnableUserSkills = true,
            EnableProjectSkills = true,
            MaxOutputSizeBytes = 51200,
            SkillsCacheDurationSeconds = 300
        };

        // Act
        var errors = settings.Validate();

        // Assert
        errors.Should().BeEmpty("valid configuration should have no validation errors");
    }

    /// <summary>
    /// Test 2.2.3: Verify Validate() returns error when MaxOutputSizeBytes is zero.
    /// </summary>
    [Fact]
    public void Validate_WithZeroMaxOutputSize_ShouldReturnError()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            MaxOutputSizeBytes = 0
        };

        // Act
        var errors = settings.Validate().ToList();

        // Assert
        errors.Should().ContainSingle("should have exactly one error");
        errors[0].Should().Be("MaxOutputSizeBytes must be greater than 0");
    }

    /// <summary>
    /// Test 2.2.5: Verify Validate() returns error when MaxOutputSizeBytes is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeMaxOutputSize_ShouldReturnError()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            MaxOutputSizeBytes = -1
        };

        // Act
        var errors = settings.Validate().ToList();

        // Assert
        errors.Should().ContainSingle("should have exactly one error");
        errors[0].Should().Be("MaxOutputSizeBytes must be greater than 0");
    }

    /// <summary>
    /// Test 2.2.5: Verify Validate() returns error when SkillsCacheDurationSeconds is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeCacheDuration_ShouldReturnError()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            SkillsCacheDurationSeconds = -1
        };

        // Act
        var errors = settings.Validate().ToList();

        // Assert
        errors.Should().ContainSingle("should have exactly one error");
        errors[0].Should().Be("SkillsCacheDurationSeconds must be non-negative");
    }

    /// <summary>
    /// Test 2.2.3: Verify Validate() returns error when both skill sources are disabled.
    /// </summary>
    [Fact]
    public void Validate_WithBothSkillSourcesDisabled_ShouldReturnError()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            EnableUserSkills = false,
            EnableProjectSkills = false
        };

        // Act
        var errors = settings.Validate().ToList();

        // Assert
        errors.Should().ContainSingle("should have exactly one error");
        errors[0].Should().Be("At least one of EnableUserSkills or EnableProjectSkills must be true");
    }

    /// <summary>
    /// Test 2.2.3: Verify Validate() returns multiple errors for multiple invalid values.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleInvalidValues_ShouldReturnMultipleErrors()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            EnableUserSkills = false,
            EnableProjectSkills = false,
            MaxOutputSizeBytes = 0,
            SkillsCacheDurationSeconds = -1
        };

        // Act
        var errors = settings.Validate().ToList();

        // Assert
        errors.Should().HaveCount(3, "should have three validation errors");
        errors.Should().Contain("MaxOutputSizeBytes must be greater than 0");
        errors.Should().Contain("SkillsCacheDurationSeconds must be non-negative");
        errors.Should().Contain("At least one of EnableUserSkills or EnableProjectSkills must be true");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetUserSkillsDirectory() returns default path when UserSkillsDir is null.
    /// </summary>
    [Fact]
    public void GetUserSkillsDirectory_WithNullUserSkillsDir_ShouldReturnDefaultPath()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            UserSkillsDir = null
        };

        // Act
        var path = settings.GetUserSkillsDirectory();

        // Assert
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".botsharp",
            "skills"
        );
        path.Should().Be(expectedPath, "should return default user skills directory");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetUserSkillsDirectory() returns custom path when UserSkillsDir is set.
    /// </summary>
    [Fact]
    public void GetUserSkillsDirectory_WithCustomUserSkillsDir_ShouldReturnCustomPath()
    {
        // Arrange
        var customPath = "/custom/user/skills";
        var settings = new AgentSkillsSettings
        {
            UserSkillsDir = customPath
        };

        // Act
        var path = settings.GetUserSkillsDirectory();

        // Assert
        path.Should().Be(customPath, "should return custom user skills directory");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetProjectSkillsDirectory() returns default path when ProjectSkillsDir is null.
    /// </summary>
    [Fact]
    public void GetProjectSkillsDirectory_WithNullProjectSkillsDir_ShouldReturnDefaultPath()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            ProjectSkillsDir = null
        };

        // Act
        var path = settings.GetProjectSkillsDirectory();

        // Assert
        var expectedPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ".botsharp",
            "skills"
        );
        path.Should().Be(expectedPath, "should return default project skills directory");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetProjectSkillsDirectory() returns custom path when ProjectSkillsDir is set.
    /// </summary>
    [Fact]
    public void GetProjectSkillsDirectory_WithCustomProjectSkillsDir_ShouldReturnCustomPath()
    {
        // Arrange
        var customPath = "/custom/project/skills";
        var settings = new AgentSkillsSettings
        {
            ProjectSkillsDir = customPath
        };

        // Act
        var path = settings.GetProjectSkillsDirectory();

        // Assert
        path.Should().Be(customPath, "should return custom project skills directory");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetProjectSkillsDirectory() uses provided projectRoot parameter.
    /// </summary>
    [Fact]
    public void GetProjectSkillsDirectory_WithProjectRootParameter_ShouldUseProvidedRoot()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            ProjectSkillsDir = null
        };
        var projectRoot = "/custom/project/root";

        // Act
        var path = settings.GetProjectSkillsDirectory(projectRoot);

        // Assert
        var expectedPath = Path.Combine(projectRoot, ".botsharp", "skills");
        path.Should().Be(expectedPath, "should use provided project root");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetUserSkillPath() returns correct path for a skill.
    /// </summary>
    [Fact]
    public void GetUserSkillPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var settings = new AgentSkillsSettings();
        var skillName = "test-skill";

        // Act
        var path = settings.GetUserSkillPath(skillName);

        // Assert
        var expectedPath = Path.Combine(
            settings.GetUserSkillsDirectory(),
            skillName
        );
        path.Should().Be(expectedPath, "should return correct user skill path");
    }

    /// <summary>
    /// Test 2.2.4: Verify GetProjectSkillPath() returns correct path for a skill.
    /// </summary>
    [Fact]
    public void GetProjectSkillPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var settings = new AgentSkillsSettings();
        var skillName = "test-skill";

        // Act
        var path = settings.GetProjectSkillPath(skillName);

        // Assert
        var expectedPath = Path.Combine(
            settings.GetProjectSkillsDirectory(),
            skillName
        );
        path.Should().Be(expectedPath, "should return correct project skill path");
    }

    /// <summary>
    /// Test 2.2.5: Verify zero cache duration is valid (permanent cache).
    /// </summary>
    [Fact]
    public void Validate_WithZeroCacheDuration_ShouldBeValid()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            SkillsCacheDurationSeconds = 0
        };

        // Act
        var errors = settings.Validate();

        // Assert
        errors.Should().BeEmpty("zero cache duration means permanent cache and should be valid");
    }

    /// <summary>
    /// Test 2.2.5: Verify boundary value for MaxOutputSizeBytes (1 byte).
    /// </summary>
    [Fact]
    public void Validate_WithMinimumMaxOutputSize_ShouldBeValid()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            MaxOutputSizeBytes = 1
        };

        // Act
        var errors = settings.Validate();

        // Assert
        errors.Should().BeEmpty("1 byte is the minimum valid value");
    }

    /// <summary>
    /// Test 2.2.5: Verify large MaxOutputSizeBytes value is valid.
    /// </summary>
    [Fact]
    public void Validate_WithLargeMaxOutputSize_ShouldBeValid()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            MaxOutputSizeBytes = 10 * 1024 * 1024 // 10MB
        };

        // Act
        var errors = settings.Validate();

        // Assert
        errors.Should().BeEmpty("large values should be valid");
    }

    /// <summary>
    /// Test 2.2.3: Verify only EnableUserSkills enabled is valid.
    /// </summary>
    [Fact]
    public void Validate_WithOnlyUserSkillsEnabled_ShouldBeValid()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            EnableUserSkills = true,
            EnableProjectSkills = false
        };

        // Act
        var errors = settings.Validate();

        // Assert
        errors.Should().BeEmpty("having only user skills enabled should be valid");
    }

    /// <summary>
    /// Test 2.2.3: Verify only EnableProjectSkills enabled is valid.
    /// </summary>
    [Fact]
    public void Validate_WithOnlyProjectSkillsEnabled_ShouldBeValid()
    {
        // Arrange
        var settings = new AgentSkillsSettings
        {
            EnableUserSkills = false,
            EnableProjectSkills = true
        };

        // Act
        var errors = settings.Validate();

        // Assert
        errors.Should().BeEmpty("having only project skills enabled should be valid");
    }
}
