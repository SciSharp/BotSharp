namespace BotSharp.Plugin.AgentSkills.Tests;

/// <summary>
/// Tests to verify the test project setup is correct
/// </summary>
public class TestSetupTests : TestBase
{
    [Fact]
    public void TestProject_ShouldHaveConfiguration()
    {
        // Arrange & Act
        var config = Configuration;

        // Assert
        config.Should().NotBeNull();
        config.GetSection("AgentSkills").Should().NotBeNull();
    }

    [Fact]
    public void TestProject_ShouldHaveTestSkillsDirectory()
    {
        // Arrange & Act
        var exists = Directory.Exists(TestSkillsDirectory);

        // Assert
        exists.Should().BeTrue($"Test skills directory should exist: {TestSkillsDirectory}");
    }

    [Theory]
    [InlineData("valid-skill")]
    [InlineData("minimal-skill")]
    [InlineData("skill-with-scripts")]
    [InlineData("large-content-skill")]
    public void TestSkills_ShouldExist(string skillName)
    {
        // Arrange & Act & Assert
        AssertTestSkillExists(skillName);
    }

    [Fact]
    public void ValidSkill_ShouldHaveAllDirectories()
    {
        // Arrange
        var skillPath = GetTestSkillPath("valid-skill");

        // Act & Assert
        Directory.Exists(Path.Combine(skillPath, "scripts")).Should().BeTrue();
        Directory.Exists(Path.Combine(skillPath, "references")).Should().BeTrue();
        Directory.Exists(Path.Combine(skillPath, "assets")).Should().BeTrue();
    }

    [Fact]
    public void MinimalSkill_ShouldOnlyHaveSkillMd()
    {
        // Arrange
        var skillPath = GetTestSkillPath("minimal-skill");

        // Act
        var directories = Directory.GetDirectories(skillPath);
        var files = Directory.GetFiles(skillPath);

        // Assert
        directories.Should().BeEmpty("minimal-skill should not have subdirectories");
        files.Should().ContainSingle(f => Path.GetFileName(f) == "SKILL.md");
    }

    [Fact]
    public void SkillWithScripts_ShouldHaveScriptsDirectory()
    {
        // Arrange
        var skillPath = GetTestSkillPath("skill-with-scripts");
        var scriptsPath = Path.Combine(skillPath, "scripts");

        // Act
        var scriptFiles = Directory.GetFiles(scriptsPath);

        // Assert
        Directory.Exists(scriptsPath).Should().BeTrue();
        scriptFiles.Should().NotBeEmpty();
        scriptFiles.Should().Contain(f => f.EndsWith(".py"));
        scriptFiles.Should().Contain(f => f.EndsWith(".sh"));
    }

    [Fact]
    public void LargeContentSkill_ShouldExceedSizeLimit()
    {
        // Arrange
        var skillPath = GetTestSkillPath("large-content-skill");
        var skillFile = Path.Combine(skillPath, "SKILL.md");
        var maxSize = 51200; // 50KB

        // Act
        var fileInfo = new FileInfo(skillFile);

        // Assert
        fileInfo.Exists.Should().BeTrue();
        fileInfo.Length.Should().BeGreaterThan(maxSize, 
            "large-content-skill SKILL.md should exceed 50KB for testing");
    }

    [Fact]
    public void ServiceProvider_ShouldBeConfigured()
    {
        // Arrange & Act
        var logger = GetService<ILogger<TestSetupTests>>();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_ShouldHaveAgentSkillsSettings()
    {
        // Arrange
        var section = Configuration.GetSection("AgentSkills");

        // Act
        var enableProjectSkills = section.GetValue<bool>("EnableProjectSkills");
        var maxOutputSize = section.GetValue<int>("MaxOutputSizeBytes");

        // Assert
        enableProjectSkills.Should().BeTrue();
        maxOutputSize.Should().Be(51200);
    }
}
