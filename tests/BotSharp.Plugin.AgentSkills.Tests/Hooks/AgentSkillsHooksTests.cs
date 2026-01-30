using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.AgentSkills.Hooks;
using BotSharp.Plugin.AgentSkills.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace BotSharp.Plugin.AgentSkills.Tests.Hooks;

/// <summary>
/// Tests for Agent Skills hooks
/// Implements requirement: NFR-2.3
/// Tests requirements: FR-2.1, FR-2.2, FR-3.1
/// </summary>
public class AgentSkillsHooksTests
{
    #region AgentSkillsInstructionHook Tests

    /// <summary>
    /// Test 5.3.1: 测试 AgentSkillsInstructionHook 指令注入成功
    /// Implements requirement: FR-2.1
    /// </summary>
    [Fact]
    public void OnInstructionLoaded_ShouldInjectSkills_WhenSkillsAvailable()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        var expectedInstructions = @"<available_skills>
  <skill>
    <name>test-skill</name>
    <description>A test skill</description>
  </skill>
</available_skills>";

        mockSkillService.Setup(s => s.GetInstructions())
            .Returns(expectedInstructions);
        mockSkillService.Setup(s => s.GetSkillCount())
            .Returns(1);

        var hook = new AgentSkillsInstructionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        // Create a Task agent (should receive skills)
        var agent = new Agent
        {
            Id = "test-agent-1",
            Name = "Test Agent",
            Type = AgentType.Task
        };
        hook.SetAgent(agent);

        var dict = new Dictionary<string, object>();

        // Act
        var result = hook.OnInstructionLoaded("template", dict);

        // Assert
        result.Should().BeTrue();
        dict.Should().ContainKey("available_skills");
        dict["available_skills"].Should().Be(expectedInstructions);

        // Verify GetInstructions was called
        mockSkillService.Verify(s => s.GetInstructions(), Times.Once);
        mockSkillService.Verify(s => s.GetSkillCount(), Times.Once);
    }

    /// <summary>
    /// Test 5.3.2: 测试 Agent 类型过滤（Routing, Planning 应跳过）
    /// Implements requirement: FR-2.2
    /// </summary>
    [Theory]
    [InlineData(AgentType.Routing)]
    [InlineData(AgentType.Planning)]
    public void OnInstructionLoaded_ShouldSkipInjection_ForRoutingAndPlanningAgents(string agentType)
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

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
        var result = hook.OnInstructionLoaded("template", dict);

        // Assert
        result.Should().BeTrue();
        dict.Should().NotContainKey("available_skills");

        // Verify GetInstructions was NOT called
        mockSkillService.Verify(s => s.GetInstructions(), Times.Never);
    }

    /// <summary>
    /// Test 5.3.3: 测试其他 Agent 类型正常注入
    /// Implements requirement: FR-2.1
    /// </summary>
    [Theory]
    [InlineData(AgentType.Task)]
    [InlineData(AgentType.Static)]
    [InlineData(AgentType.Evaluating)]
    [InlineData(AgentType.A2ARemote)]
    public void OnInstructionLoaded_ShouldInjectSkills_ForNonRoutingPlanningAgents(string agentType)
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
        var result = hook.OnInstructionLoaded("template", dict);

        // Assert
        result.Should().BeTrue();
        dict.Should().ContainKey("available_skills");
        dict["available_skills"].Should().Be(expectedInstructions);

        // Verify GetInstructions was called
        mockSkillService.Verify(s => s.GetInstructions(), Times.Once);
    }

    /// <summary>
    /// Test 5.3.4: 测试 XML 格式正确性（验证 <available_skills> 标签）
    /// Implements requirement: FR-2.1
    /// </summary>
    [Fact]
    public void OnInstructionLoaded_ShouldInjectValidXmlFormat()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        var expectedInstructions = @"<available_skills>
  <skill>
    <name>pdf-processing</name>
    <description>Extracts text and tables from PDF files</description>
  </skill>
  <skill>
    <name>data-analysis</name>
    <description>Analyzes datasets and generates reports</description>
  </skill>
</available_skills>";

        mockSkillService.Setup(s => s.GetInstructions())
            .Returns(expectedInstructions);
        mockSkillService.Setup(s => s.GetSkillCount())
            .Returns(2);

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

        // Assert
        var injectedXml = dict["available_skills"] as string;
        injectedXml.Should().NotBeNullOrEmpty();
        injectedXml.Should().Contain("<available_skills>");
        injectedXml.Should().Contain("</available_skills>");
        injectedXml.Should().Contain("<skill>");
        injectedXml.Should().Contain("</skill>");
        injectedXml.Should().Contain("<name>");
        injectedXml.Should().Contain("<description>");
    }

    /// <summary>
    /// Test: Handle empty instructions gracefully
    /// Implements requirement: FR-1.3
    /// </summary>
    [Fact]
    public void OnInstructionLoaded_ShouldHandleEmptyInstructions()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        mockSkillService.Setup(s => s.GetInstructions())
            .Returns(string.Empty);

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
        var result = hook.OnInstructionLoaded("template", dict);

        // Assert
        result.Should().BeTrue();
        dict.Should().NotContainKey("available_skills");
    }

    /// <summary>
    /// Test: Handle exception during injection
    /// Implements requirement: FR-1.3
    /// </summary>
    [Fact]
    public void OnInstructionLoaded_ShouldHandleException_AndNotThrow()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsInstructionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        mockSkillService.Setup(s => s.GetInstructions())
            .Throws(new InvalidOperationException("Test exception"));

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
        var act = () => hook.OnInstructionLoaded("template", dict);

        // Assert
        act.Should().NotThrow();
        dict.Should().NotContainKey("available_skills");
    }

    #endregion

    #region AgentSkillsFunctionHook Tests

    /// <summary>
    /// Test 5.3.5: 测试 AgentSkillsFunctionHook 函数注册成功
    /// Implements requirement: FR-3.1
    /// </summary>
    [Fact]
    public void OnFunctionsLoaded_ShouldRegisterTools_WhenToolsAvailable()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        // Create mock AIFunction tools
        var mockAIFunction1 = CreateMockAIFunction("read_skill", "Read a skill's SKILL.md content");
        var mockAIFunction2 = CreateMockAIFunction("read_skill_file", "Read a file from a skill directory");

        var tools = new List<AITool> { mockAIFunction1, mockAIFunction2 };

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
        var result = hook.OnFunctionsLoaded(functions);

        // Assert
        result.Should().BeTrue();
        functions.Should().HaveCount(2);
        functions.Should().Contain(f => f.Name == "read_skill");
        functions.Should().Contain(f => f.Name == "read_skill_file");

        // Verify GetTools was called
        mockSkillService.Verify(s => s.GetTools(), Times.Once);
    }

    /// <summary>
    /// Test 5.3.6: 测试参数转换正确性（FunctionParametersDef）
    /// Implements requirement: FR-3.1
    /// </summary>
    [Fact]
    public void OnFunctionsLoaded_ShouldConvertParametersCorrectly()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        // Create mock AIFunction with parameters
        var additionalProperties = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = JsonDocument.Parse(@"{
                ""skill_name"": {
                    ""type"": ""string"",
                    ""description"": ""Name of the skill""
                }
            }").RootElement,
            ["required"] = JsonDocument.Parse(@"[""skill_name""]").RootElement
        };

        var mockAIFunction = CreateMockAIFunction("read_skill", "Read skill content", additionalProperties);
        var tools = new List<AITool> { mockAIFunction };

        mockSkillService.Setup(s => s.GetTools())
            .Returns(tools);

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent { Id = "test-agent", Type = AgentType.Task };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act
        hook.OnFunctionsLoaded(functions);

        // Assert
        functions.Should().HaveCount(1);
        var func = functions[0];
        func.Parameters.Should().NotBeNull();
        func.Parameters!.Type.Should().Be("object");
        func.Parameters.Required.Should().Contain("skill_name");
    }

    /// <summary>
    /// Test 5.3.7: 测试重复注册防护
    /// Implements requirement: NFR-2.1
    /// </summary>
    [Fact]
    public void OnFunctionsLoaded_ShouldPreventDuplicateRegistration()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        var mockAIFunction = CreateMockAIFunction("read_skill", "Read skill content");
        var tools = new List<AITool> { mockAIFunction };

        mockSkillService.Setup(s => s.GetTools())
            .Returns(tools);

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent { Id = "test-agent", Type = AgentType.Task };
        hook.SetAgent(agent);

        // Pre-populate functions with a function that has the same name
        var functions = new List<FunctionDef>
        {
            new FunctionDef
            {
                Name = "read_skill",
                Description = "Existing function"
            }
        };

        // Act
        hook.OnFunctionsLoaded(functions);

        // Assert
        functions.Should().HaveCount(1); // Should not add duplicate
        functions[0].Description.Should().Be("Existing function"); // Original should remain
    }

    /// <summary>
    /// Test 5.3.8: 测试错误处理（GetTools 失败）
    /// Implements requirement: FR-1.3
    /// </summary>
    [Fact]
    public void OnFunctionsLoaded_ShouldHandleException_AndNotThrow()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        mockSkillService.Setup(s => s.GetTools())
            .Throws(new InvalidOperationException("Test exception"));

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent { Id = "test-agent", Type = AgentType.Task };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act
        var act = () => hook.OnFunctionsLoaded(functions);

        // Assert
        act.Should().NotThrow();
        functions.Should().BeEmpty();
    }

    /// <summary>
    /// Test: Handle null or empty tools list
    /// </summary>
    [Fact]
    public void OnFunctionsLoaded_ShouldHandleEmptyToolsList()
    {
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

        var agent = new Agent { Id = "test-agent", Type = AgentType.Task };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act
        var result = hook.OnFunctionsLoaded(functions);

        // Assert
        result.Should().BeTrue();
        functions.Should().BeEmpty();
    }

    /// <summary>
    /// Test: Handle non-AIFunction tools
    /// </summary>
    [Fact]
    public void OnFunctionsLoaded_ShouldSkipNonAIFunctionTools()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillService>();
        var mockLogger = new Mock<ILogger<AgentSkillsFunctionHook>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var agentSettings = new AgentSettings();

        // Create a mock AITool that is not an AIFunction
        var mockTool = new Mock<AITool>();
        var tools = new List<AITool> { mockTool.Object };

        mockSkillService.Setup(s => s.GetTools())
            .Returns(tools);

        var hook = new AgentSkillsFunctionHook(
            mockServiceProvider.Object,
            agentSettings,
            mockSkillService.Object,
            mockLogger.Object);

        var agent = new Agent { Id = "test-agent", Type = AgentType.Task };
        hook.SetAgent(agent);

        var functions = new List<FunctionDef>();

        // Act
        hook.OnFunctionsLoaded(functions);

        // Assert
        functions.Should().BeEmpty(); // Non-AIFunction tools should be skipped
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create a mock AIFunction
    /// Test 5.3.9: 使用 Moq 模拟 ISkillService 和 Agent
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
