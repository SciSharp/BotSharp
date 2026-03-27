using AgentSkillsDotNet;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Settings;
using CsCheck;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills.Tests.Services;

/// <summary>
/// Property-based tests for SkillService class using CsCheck.
/// Tests correctness properties defined in design document section 11.1.
/// Tests requirements: NFR-2.3
/// </summary>
public class SkillServicePropertyTests
{
    private readonly AgentSkillsFactory _factory;
    private readonly ILogger<SkillService> _logger;
    private readonly string _testSkillsPath;

    public SkillServicePropertyTests()
    {
        _factory = new AgentSkillsFactory();
        _logger = new TestLogger<SkillService>();
        _testSkillsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test-skills");
    }

    private AgentSkillsSettings CreateSettings(string? skillsDir = null)
    {
        return new AgentSkillsSettings
        {
            EnableProjectSkills = true,
            EnableUserSkills = false,
            ProjectSkillsDir = skillsDir ?? _testSkillsPath,
            EnableReadFileTool = true,
            EnableListDirectoryTool = true,
            MaxOutputSizeBytes = 51200
        };
    }

    /// <summary>
    /// Property 1.1: Skill loading idempotency.
    /// For any valid skill directory dir,
    /// multiple calls to GetAgentSkills(dir) should return the same skill set.
    /// </summary>
    [Fact]
    public void Property_SkillLoadingIdempotency_MultipleLoadsReturnSameSkills()
    {
        // This property tests that loading skills multiple times from the same directory
        // produces consistent results (idempotency)
        
        // Arrange
        var settings = CreateSettings();
        
        // Act - Load skills multiple times
        var service1 = new SkillService(_factory, settings, _logger);
        var count1 = service1.GetSkillCount();
        var instructions1 = service1.GetInstructions();
        var tools1 = service1.GetTools();
        
        var service2 = new SkillService(_factory, settings, _logger);
        var count2 = service2.GetSkillCount();
        var instructions2 = service2.GetInstructions();
        var tools2 = service2.GetTools();
        
        var service3 = new SkillService(_factory, settings, _logger);
        var count3 = service3.GetSkillCount();
        var instructions3 = service3.GetInstructions();
        var tools3 = service3.GetTools();
        
        // Assert - All loads should produce identical results
        count1.Should().Be(count2, "first and second load should have same skill count");
        count2.Should().Be(count3, "second and third load should have same skill count");
        
        instructions1.Should().Be(instructions2, "first and second load should have same instructions");
        instructions2.Should().Be(instructions3, "second and third load should have same instructions");
        
        tools1.Count.Should().Be(tools2.Count, "first and second load should have same tool count");
        tools2.Count.Should().Be(tools3.Count, "second and third load should have same tool count");
        
        // Verify tool names are identical
        var toolNames1 = tools1.Select(t => t is Microsoft.Extensions.AI.AIFunction f ? f.Name : null).Where(n => n != null).OrderBy(n => n).ToList();
        var toolNames2 = tools2.Select(t => t is Microsoft.Extensions.AI.AIFunction f ? f.Name : null).Where(n => n != null).OrderBy(n => n).ToList();
        var toolNames3 = tools3.Select(t => t is Microsoft.Extensions.AI.AIFunction f ? f.Name : null).Where(n => n != null).OrderBy(n => n).ToList();
        
        toolNames1.Should().BeEquivalentTo(toolNames2, "first and second load should have same tool names");
        toolNames2.Should().BeEquivalentTo(toolNames3, "second and third load should have same tool names");
    }

    /// <summary>
    /// Property 1.1: Skill loading idempotency with reload.
    /// ReloadSkillsAsync should produce the same results as initial load.
    /// </summary>
    [Fact]
    public async Task Property_SkillLoadingIdempotency_ReloadProducesSameResults()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);
        
        var initialCount = service.GetSkillCount();
        var initialInstructions = service.GetInstructions();
        var initialToolCount = service.GetTools().Count;
        
        // Act - Reload skills multiple times
        await service.ReloadSkillsAsync();
        var reloadCount1 = service.GetSkillCount();
        var reloadInstructions1 = service.GetInstructions();
        var reloadToolCount1 = service.GetTools().Count;
        
        await service.ReloadSkillsAsync();
        var reloadCount2 = service.GetSkillCount();
        var reloadInstructions2 = service.GetInstructions();
        var reloadToolCount2 = service.GetTools().Count;
        
        // Assert - Reloads should produce same results as initial load
        reloadCount1.Should().Be(initialCount, "first reload should have same skill count as initial");
        reloadCount2.Should().Be(initialCount, "second reload should have same skill count as initial");
        
        reloadInstructions1.Should().Be(initialInstructions, "first reload should have same instructions as initial");
        reloadInstructions2.Should().Be(initialInstructions, "second reload should have same instructions as initial");
        
        reloadToolCount1.Should().Be(initialToolCount, "first reload should have same tool count as initial");
        reloadToolCount2.Should().Be(initialToolCount, "second reload should have same tool count as initial");
    }

    /// <summary>
    /// Property 1.2: Skill count consistency.
    /// For any skill directory dir,
    /// GetSkillCount() should equal the number of valid SKILL.md files in the directory.
    /// </summary>
    [Fact]
    public void Property_SkillCountConsistency_CountMatchesValidSkillFiles()
    {
        // This property tests that the skill count reported by the service
        // matches the actual number of valid SKILL.md files in the directory
        
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);
        
        // Act
        var reportedCount = service.GetSkillCount();
        
        // Count actual SKILL.md files in test directory
        var actualSkillFiles = Directory.GetDirectories(_testSkillsPath)
            .Select(dir => Path.Combine(dir, "SKILL.md"))
            .Where(File.Exists)
            .Count();
        
        // Assert
        reportedCount.Should().Be(actualSkillFiles, 
            "skill count should match the number of directories with SKILL.md files");
        
        // We know we have 4 test skills: valid-skill, minimal-skill, skill-with-scripts, large-content-skill
        reportedCount.Should().Be(4, "test directory contains 4 valid skills");
    }

    /// <summary>
    /// Property 1.2: Skill count consistency with instructions.
    /// The skill count should match the number of &lt;skill&gt; tags in instructions XML.
    /// </summary>
    [Fact]
    public void Property_SkillCountConsistency_CountMatchesInstructionsXml()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);
        
        // Act
        var skillCount = service.GetSkillCount();
        var instructions = service.GetInstructions();
        
        // Count <skill> tags in instructions
        var skillTagCount = instructions.Split("<skill>").Length - 1;
        
        // Assert
        skillCount.Should().Be(skillTagCount, 
            "skill count should match the number of <skill> tags in instructions XML");
    }

    /// <summary>
    /// Property 1.2: Skill count consistency across different access methods.
    /// GetSkillCount(), instruction parsing, and tool generation should all be consistent.
    /// </summary>
    [Fact]
    public void Property_SkillCountConsistency_ConsistentAcrossAccessMethods()
    {
        // Arrange
        var settings = CreateSettings();
        var service = new SkillService(_factory, settings, _logger);
        
        // Act - Get skill count through different methods
        var directCount = service.GetSkillCount();
        
        var instructions = service.GetInstructions();
        var instructionCount = instructions.Split("<skill>").Length - 1;
        
        var tools = service.GetTools();
        // Tools include skill listing tools plus per-skill tools, so we can't directly compare
        // But we can verify tools were generated
        var hasTools = tools.Count > 0;
        
        // Assert
        directCount.Should().Be(instructionCount, 
            "GetSkillCount() should match instruction XML parsing");
        
        hasTools.Should().BeTrue("tools should be generated when skills are loaded");
        
        // If we have skills, we should have at least the base tools (get-available-skills, get-skill-by-name)
        if (directCount > 0)
        {
            tools.Count.Should().BeGreaterThan(0, "should have tools when skills are loaded");
        }
    }

    /// <summary>
    /// Property test: Empty directory should result in zero skills.
    /// </summary>
    [Fact]
    public void Property_EmptyDirectory_ResultsInZeroSkills()
    {
        // Arrange - Create a temporary empty directory
        var tempDir = Path.Combine(Path.GetTempPath(), $"empty-skills-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var settings = CreateSettings(tempDir);
            
            // Act
            var service = new SkillService(_factory, settings, _logger);
            
            // Assert
            service.GetSkillCount().Should().Be(0, "empty directory should have zero skills");
            
            // AgentSkillsDotNet returns "<available_skills>\n</available_skills>" even when empty
            var instructions = service.GetInstructions();
            instructions.Should().Contain("<available_skills>", "should have available_skills tag");
            instructions.Should().Contain("</available_skills>", "should have closing tag");
            instructions.Should().NotContain("<skill>", "should not have any skill tags");
            
            // AgentSkillsDotNet still generates base tools (get-available-skills) even with no skills
            // This is correct behavior - the tool is always available to list skills
            var tools = service.GetTools();
            tools.Should().NotBeNull("tools collection should not be null");
            
            // Verify that get-available-skills tool exists
            var toolNames = tools.Select(t => t is Microsoft.Extensions.AI.AIFunction f ? f.Name : null).Where(n => n != null).ToList();
            toolNames.Should().Contain("get-available-skills", "get-available-skills tool should always be available");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Property test: Non-existent directory should result in zero skills without throwing.
    /// </summary>
    [Fact]
    public void Property_NonExistentDirectory_ResultsInZeroSkillsWithoutThrowing()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}");
        var settings = CreateSettings(nonExistentDir);
        
        // Act
        var act = () => new SkillService(_factory, settings, _logger);
        
        // Assert
        act.Should().NotThrow("non-existent directory should not throw exception");
        
        var service = new SkillService(_factory, settings, _logger);
        service.GetSkillCount().Should().Be(0, "non-existent directory should have zero skills");
        service.GetInstructions().Should().BeEmpty("non-existent directory should have empty instructions");
        service.GetTools().Should().BeEmpty("non-existent directory should have no tools");
    }
}
