using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.AgentSkills.Hooks;
using BotSharp.Plugin.AgentSkills.Services;
using CsCheck;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Xml.Linq;

namespace BotSharp.Plugin.AgentSkills.Tests.Hooks;

/// <summary>
/// Property-based tests for Agent Skills hooks using CsCheck.
/// Tests correctness properties defined in design document sections 11.5 and 11.2.
/// Implements requirement: NFR-2.3
/// Tests requirements: FR-2.1, FR-2.2, FR-3.1
/// </summary>
public class AgentSkillsHooksPropertyTests
{
    #region Property 5.1: Agent Type Filtering

    /// <summary>
    /// Property 5.1: Agent type filtering.
    /// For any Agent agent,
    /// IF agent.Type IN [Routing, Planning],
    /// THEN OnInstructionLoaded() should not inject available_skills.
    /// 
    /// Implements requirement: FR-2.2
    /// Design reference: 11.5
    /// </summary>
    [Fact]
    public void Property_AgentTypeFiltering_RoutingAndPlanningAgentsSkipInjection()
    {
        // This property tests that Routing and Planning agents never receive skill injection
        // regardless of other conditions
        
        // Define the agent types that should be filtered
        var filteredTypes = new[] { AgentType.Routing, AgentType.Planning };
        
        // Test each filtered type
        foreach (var agentType in filteredTypes)
        {
            // Arrange
            var mockSkillService = new Mock<ISkillService>();
            var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var agentSettings = new AgentSettings();

            // Setup skill service to return valid instructions
            mockSkillService.Setup(s => s.GetInstructions())
                .Returns("<available_skills><skill><name>test</name></skill></available_skills>");
            mockSkillService.Setup(s => s.GetSkillCount())
                .Returns(1);

            var hook = new AgentSkillsInstructionHook(
                mockServiceProvider.Object,
                agentSettings,
                mockSkillService.Object,
                mockLogger.Object);

            // Create agent with filtered type
            var agent = new Agent
            {
                Id = $"test-agent-{agentType}",
                Name = $"Test {agentType} Agent",
                Type = agentType
            };
            hook.SetAgent(agent);

            var dict = new Dictionary<string, object>();

            // Act
            hook.OnInstructionLoaded("template", dict);

            // Assert - Property: Filtered agents should NOT have available_skills injected
            dict.Should().NotContainKey("available_skills",
                $"Agent type {agentType} should not receive skill injection");

            // Verify GetInstructions was NOT called for filtered types
            mockSkillService.Verify(s => s.GetInstructions(), Times.Never,
                $"GetInstructions should not be called for {agentType} agents");
        }
    }

    /// <summary>
    /// Property 5.1: Agent type filtering (inverse).
    /// For any Agent agent,
    /// IF agent.Type NOT IN [Routing, Planning],
    /// THEN OnInstructionLoaded() should inject available_skills (when skills are available).
    /// 
    /// Implements requirement: FR-2.1
    /// Design reference: 11.5
    /// </summary>
    [Fact]
    public void Property_AgentTypeFiltering_NonFilteredAgentsReceiveInjection()
    {
        // This property tests that all non-filtered agent types receive skill injection
        
        // Define agent types that should receive injection
        var nonFilteredTypes = new[]
        {
            AgentType.Task,
            AgentType.Static,
            AgentType.Evaluating,
            AgentType.A2ARemote
        };
        
        // Test each non-filtered type
        foreach (var agentType in nonFilteredTypes)
        {
            // Arrange
            var mockSkillService = new Mock<ISkillService>();
            var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var agentSettings = new AgentSettings();

            var expectedInstructions = "<available_skills><skill><name>test</name></skill></available_skills>";

            mockSkillService.Setup(s => s.GetInstructions())
                .Returns(expectedInstructions);
            mockSkillService.Setup(s => s.GetSkillCount())
                .Returns(1);

            var hook = new AgentSkillsInstructionHook(
                mockServiceProvider.Object,
                agentSettings,
                mockSkillService.Object,
                mockLogger.Object);

            var agent = new Agent
            {
                Id = $"test-agent-{agentType}",
                Name = $"Test {agentType} Agent",
                Type = agentType
            };
            hook.SetAgent(agent);

            var dict = new Dictionary<string, object>();

            // Act
            hook.OnInstructionLoaded("template", dict);

            // Assert - Property: Non-filtered agents should have available_skills injected
            dict.Should().ContainKey("available_skills",
                $"Agent type {agentType} should receive skill injection");
            dict["available_skills"].Should().Be(expectedInstructions,
                $"Agent type {agentType} should receive correct instructions");

            // Verify GetInstructions was called for non-filtered types
            mockSkillService.Verify(s => s.GetInstructions(), Times.Once,
                $"GetInstructions should be called for {agentType} agents");
        }
    }

    /// <summary>
    /// Property 5.1: Agent type filtering is consistent across multiple invocations.
    /// The filtering behavior should be deterministic and consistent.
    /// </summary>
    [Fact]
    public void Property_AgentTypeFiltering_ConsistentAcrossInvocations()
    {
        // This property tests that the filtering behavior is consistent
        // when the same hook is invoked multiple times
        
        var testCases = new[]
        {
            (AgentType.Routing, false),    // Should NOT inject
            (AgentType.Planning, false),   // Should NOT inject
            (AgentType.Task, true),        // Should inject
            (AgentType.Static, true)       // Should inject
        };
        
        foreach (var (agentType, shouldInject) in testCases)
        {
            // Arrange
            var mockSkillService = new Mock<ISkillService>();
            var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var agentSettings = new AgentSettings();

            mockSkillService.Setup(s => s.GetInstructions())
                .Returns("<available_skills><skill><name>test</name></skill></available_skills>");
            mockSkillService.Setup(s => s.GetSkillCount())
                .Returns(1);

            var hook = new AgentSkillsInstructionHook(
                mockServiceProvider.Object,
                agentSettings,
                mockSkillService.Object,
                mockLogger.Object);

            var agent = new Agent
            {
                Id = $"test-agent-{agentType}",
                Type = agentType
            };
            hook.SetAgent(agent);

            // Act - Invoke multiple times
            var results = new List<bool>();
            for (int i = 0; i < 3; i++)
            {
                var dict = new Dictionary<string, object>();
                hook.OnInstructionLoaded("template", dict);
                results.Add(dict.ContainsKey("available_skills"));
            }

            // Assert - All invocations should produce the same result
            results.Should().AllBeEquivalentTo(shouldInject,
                $"Agent type {agentType} should consistently {(shouldInject ? "receive" : "not receive")} injection");
        }
    }

    #endregion

    #region Property 5.2: Instruction Format Correctness

    /// <summary>
    /// Property 5.2: Instruction format correctness.
    /// For any skill set skills,
    /// GetInstructions() should return valid XML format string.
    /// 
    /// Implements requirement: FR-2.1
    /// Design reference: 11.5
    /// </summary>
    [Fact]
    public void Property_InstructionFormat_AlwaysValidXml()
    {
        // This property tests that instructions are always in valid XML format
        
        // Test with various instruction formats
        var testInstructions = new[]
        {
            // Empty skills
            "<available_skills>\n</available_skills>",
            
            // Single skill
            "<available_skills>\n  <skill>\n    <name>test-skill</name>\n    <description>Test</description>\n  </skill>\n</available_skills>",
            
            // Multiple skills
            "<available_skills>\n  <skill>\n    <name>skill1</name>\n    <description>First</description>\n  </skill>\n  <skill>\n    <name>skill2</name>\n    <description>Second</description>\n  </skill>\n</available_skills>",
            
            // Skill with special characters in description
            "<available_skills>\n  <skill>\n    <name>special-skill</name>\n    <description>Test &amp; special &lt;chars&gt;</description>\n  </skill>\n</available_skills>"
        };
        
        foreach (var instructions in testInstructions)
        {
            // Arrange
            var mockSkillService = new Mock<ISkillService>();
            var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var agentSettings = new AgentSettings();

            mockSkillService.Setup(s => s.GetInstructions())
                .Returns(instructions);
            mockSkillService.Setup(s => s.GetSkillCount())
                .Returns(instructions.Split("<skill>").Length - 1);

            var hook = new AgentSkillsInstructionHook(
                mockServiceProvider.Object,
                agentSettings,
                mockSkillService.Object,
                mockLogger.Object);

            var agent = new Agent
            {
                Id = "test-agent",
                Type = AgentType.Task
            };
            hook.SetAgent(agent);

            var dict = new Dictionary<string, object>();

            // Act
            hook.OnInstructionLoaded("template", dict);

            // Assert - Property: Instructions should be valid XML
            if (dict.ContainsKey("available_skills"))
            {
                var injectedXml = dict["available_skills"] as string;
                injectedXml.Should().NotBeNullOrEmpty("instructions should not be empty");

                // Verify XML is parseable
                var parseXml = () => XDocument.Parse(injectedXml!);
                parseXml.Should().NotThrow("instructions should be valid XML");

                // Verify root element
                var doc = XDocument.Parse(injectedXml!);
                doc.Root.Should().NotBeNull("XML should have a root element");
                doc.Root!.Name.LocalName.Should().Be("available_skills",
                    "root element should be <available_skills>");
            }
        }
    }

    /// <summary>
    /// Property 5.2: Instruction format has required structure.
    /// The XML should always have the &lt;available_skills&gt; root element.
    /// </summary>
    [Fact]
    public void Property_InstructionFormat_HasRequiredStructure()
    {
        // This property tests that instructions always have the required XML structure
        
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        var instructions = @"<available_skills>
  <skill>
    <name>test-skill</name>
    <description>Test description</description>
  </skill>
</available_skills>";

        mockSkillService.Setup(s => s.GetInstructions())
            .Returns(instructions);
        mockSkillService.Setup(s => s.GetSkillCount())
            .Returns(1);

        var hook = new AgentSkillsInstructionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent
        {
            Id = "test-agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        var dict = new Dictionary<string, object>();

        // Act
        hook.OnInstructionLoaded("template", dict);

        // Assert - Property: Instructions should have required structure
        dict.Should().ContainKey("available_skills");
        var injectedXml = dict["available_skills"] as string;

        var doc = XDocument.Parse(injectedXml!);
        
        // Root element should be <available_skills>
        doc.Root!.Name.LocalName.Should().Be("available_skills");
        
        // Should have <skill> children
        var skills = doc.Root.Elements("skill").ToList();
        skills.Should().HaveCount(1, "should have one skill element");
        
        // Each skill should have <name> and <description>
        var skill = skills[0];
        skill.Element("name").Should().NotBeNull("skill should have name element");
        skill.Element("description").Should().NotBeNull("skill should have description element");
        
        skill.Element("name")!.Value.Should().Be("test-skill");
        skill.Element("description")!.Value.Should().Be("Test description");
    }

    /// <summary>
    /// Property 5.2: Empty instructions should not inject.
    /// When GetInstructions() returns empty or null, no injection should occur.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Property_InstructionFormat_EmptyInstructionsDoNotInject(string? emptyInstructions)
    {
        // This property tests that empty instructions don't result in injection
        
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        mockSkillService.Setup(s => s.GetInstructions())
            .Returns(emptyInstructions!);
        mockSkillService.Setup(s => s.GetSkillCount())
            .Returns(0);

        var hook = new AgentSkillsInstructionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent
        {
            Id = "test-agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        var dict = new Dictionary<string, object>();

        // Act
        hook.OnInstructionLoaded("template", dict);

        // Assert - Property: Empty instructions should not inject
        dict.Should().NotContainKey("available_skills",
            "empty instructions should not result in injection");
    }

    #endregion

    #region Property 2.1: Tool Name Uniqueness

    /// <summary>
    /// Property 2.1: Tool name uniqueness.
    /// For any skill set skills,
    /// GetAsTools(skills) returned tool names should be unique.
    /// 
    /// Implements requirement: FR-3.1
    /// Design reference: 11.2
    /// </summary>
    [Fact]
    public void Property_ToolNameUniqueness_AllToolNamesAreUnique()
    {
        // This property tests that all tool names generated by the hook are unique
        
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        // Create multiple mock tools with different names
        var tools = new List<AITool>
        {
            CreateMockAIFunction("read_skill", "Read skill content"),
            CreateMockAIFunction("read_skill_file", "Read skill file"),
            CreateMockAIFunction("list_skill_directory", "List skill directory"),
            CreateMockAIFunction("get-available-skills", "Get available skills"),
            CreateMockAIFunction("get-skill-by-name", "Get skill by name")
        };

        mockSkillService.Setup(s => s.GetTools())
            .Returns(tools);

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent
        {
            Id = "test-agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act
        hook.OnFunctionsLoaded(functions);

        // Assert - Property: All tool names should be unique
        var toolNames = functions.Select(f => f.Name).ToList();
        var uniqueNames = toolNames.Distinct().ToList();
        
        toolNames.Should().HaveCount(uniqueNames.Count,
            "all tool names should be unique (no duplicates)");
        
        // Verify each expected tool is present exactly once
        toolNames.Should().Contain("read_skill");
        toolNames.Should().Contain("read_skill_file");
        toolNames.Should().Contain("list_skill_directory");
        toolNames.Should().Contain("get-available-skills");
        toolNames.Should().Contain("get-skill-by-name");
        
        toolNames.Count(n => n == "read_skill").Should().Be(1,
            "read_skill should appear exactly once");
        toolNames.Count(n => n == "read_skill_file").Should().Be(1,
            "read_skill_file should appear exactly once");
    }

    /// <summary>
    /// Property 2.1: Tool name uniqueness with duplicate prevention.
    /// When a tool with the same name already exists, it should not be added again.
    /// </summary>
    [Fact]
    public void Property_ToolNameUniqueness_DuplicatesArePrevented()
    {
        // This property tests that the hook prevents duplicate tool registration
        
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        var tools = new List<AITool>
        {
            CreateMockAIFunction("read_skill", "Read skill content"),
            CreateMockAIFunction("read_skill_file", "Read skill file")
        };

        mockSkillService.Setup(s => s.GetTools())
            .Returns(tools);

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent
        {
            Id = "test-agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        // Pre-populate with a duplicate tool
        var functions = new List<FunctionDef>
        {
            new FunctionDef
            {
                Name = "read_skill",
                Description = "Existing read_skill function"
            }
        };

        // Act
        hook.OnFunctionsLoaded(functions);

        // Assert - Property: Duplicate should not be added
        functions.Should().HaveCount(2, "should have original + one new tool (duplicate prevented)");
        
        var toolNames = functions.Select(f => f.Name).ToList();
        toolNames.Count(n => n == "read_skill").Should().Be(1,
            "read_skill should appear exactly once (duplicate prevented)");
        toolNames.Should().Contain("read_skill_file",
            "non-duplicate tool should be added");
        
        // Verify the original description is preserved
        var readSkillFunc = functions.First(f => f.Name == "read_skill");
        readSkillFunc.Description.Should().Be("Existing read_skill function",
            "original function should be preserved when duplicate is prevented");
    }

    /// <summary>
    /// Property 2.1: Tool name uniqueness across multiple hook invocations.
    /// Multiple invocations should maintain uniqueness.
    /// </summary>
    [Fact]
    public void Property_ToolNameUniqueness_MaintainedAcrossInvocations()
    {
        // This property tests that uniqueness is maintained across multiple invocations
        
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        var tools = new List<AITool>
        {
            CreateMockAIFunction("tool1", "Tool 1"),
            CreateMockAIFunction("tool2", "Tool 2")
        };

        mockSkillService.Setup(s => s.GetTools())
            .Returns(tools);

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent
        {
            Id = "test-agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act - Invoke multiple times
        hook.OnFunctionsLoaded(functions);
        var countAfterFirst = functions.Count;
        
        hook.OnFunctionsLoaded(functions);
        var countAfterSecond = functions.Count;
        
        hook.OnFunctionsLoaded(functions);
        var countAfterThird = functions.Count;

        // Assert - Property: Count should not increase after first invocation (duplicates prevented)
        countAfterFirst.Should().Be(2, "first invocation should add 2 tools");
        countAfterSecond.Should().Be(2, "second invocation should not add duplicates");
        countAfterThird.Should().Be(2, "third invocation should not add duplicates");
        
        // Verify all names are still unique
        var toolNames = functions.Select(f => f.Name).ToList();
        var uniqueNames = toolNames.Distinct().ToList();
        toolNames.Should().HaveCount(uniqueNames.Count, "all tool names should remain unique");
    }

    /// <summary>
    /// Property 2.1: Empty tool list maintains uniqueness invariant.
    /// When no tools are available, the function list should remain valid.
    /// </summary>
    [Fact]
    public void Property_ToolNameUniqueness_EmptyToolListIsValid()
    {
        // This property tests that empty tool lists don't violate uniqueness
        
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        mockSkillService.Setup(s => s.GetTools())
            .Returns(new List<AITool>());

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent
        {
            Id = "test-agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act
        hook.OnFunctionsLoaded(functions);

        // Assert - Property: Empty list is valid and maintains uniqueness
        functions.Should().BeEmpty("no tools should be added when tool list is empty");
        
        // Uniqueness is trivially satisfied for empty list
        var toolNames = functions.Select(f => f.Name).ToList();
        var uniqueNames = toolNames.Distinct().ToList();
        toolNames.Should().HaveCount(uniqueNames.Count, "empty list satisfies uniqueness");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create a mock AIFunction for testing.
    /// </summary>
    private static AIFunction CreateMockAIFunction(
        string name,
        string description,
        IReadOnlyDictionary<string, object?>? additionalProperties = null)
    {
        additionalProperties ??= new Dictionary<string, object?>();

        var mockFunction = new Mock<AIFunction>();
        mockFunction.Setup(f => f.Name).Returns(name);
        mockFunction.Setup(f => f.Description).Returns(description);
        mockFunction.Setup(f => f.AdditionalProperties).Returns(additionalProperties);

        return mockFunction.Object;
    }

    #endregion
}
