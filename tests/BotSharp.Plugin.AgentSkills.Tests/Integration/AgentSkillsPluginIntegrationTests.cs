using AgentSkillsDotNet;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.AgentSkills.Functions;
using BotSharp.Plugin.AgentSkills.Hooks;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills.Tests.Integration;

/// <summary>
/// Integration tests for AgentSkillsPlugin.
/// Tests the complete plugin loading and initialization flow.
/// Implements requirement: NFR-2.3
/// Tests requirements: FR-1.1, FR-3.1, FR-4.1, FR-6.1
/// Design reference: 12.2
/// </summary>
public class AgentSkillsPluginIntegrationTests : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private readonly string _testSkillsPath;

    public AgentSkillsPluginIntegrationTests()
    {
        _testSkillsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test-skills");
    }

    /// <summary>
    /// Test 6.3.1: Test plugin registration - all services correctly registered to DI container.
    /// Implements requirement: FR-1.1, FR-6.1
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldRegisterAllServices_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        // Add required BotSharp services
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();

        // Act
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all services are registered
        _serviceProvider.GetService<AgentSkillsSettings>().Should().NotBeNull(
            "AgentSkillsSettings should be registered");

        _serviceProvider.GetService<AgentSkillsFactory>().Should().NotBeNull(
            "AgentSkillsFactory should be registered");

        _serviceProvider.GetService<ISkillService>().Should().NotBeNull(
            "ISkillService should be registered");

        var skillService = _serviceProvider.GetService<ISkillService>();
        skillService.Should().BeOfType<SkillService>(
            "ISkillService should be implemented by SkillService");

        // Verify hooks are registered in service collection (not resolved)
        var hookDescriptors = services.Where(d => d.ServiceType == typeof(IAgentHook)).ToList();
        hookDescriptors.Should().HaveCountGreaterThanOrEqualTo(2, "should register at least 2 hooks");
    }

    /// <summary>
    /// Test 6.3.2: Test configuration loading from IConfiguration.
    /// Implements requirement: FR-6.1
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldLoadConfiguration_FromIConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();

        // Act
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        var settings = _serviceProvider.GetRequiredService<AgentSkillsSettings>();

        // Assert - Verify configuration is loaded correctly
        settings.Should().NotBeNull();
        settings.EnableProjectSkills.Should().BeTrue("default value should be true");
        settings.EnableUserSkills.Should().BeFalse("test configuration sets this to false");
        settings.MaxOutputSizeBytes.Should().Be(51200, "default value should be 50KB");
        settings.EnableReadFileTool.Should().BeTrue("default value should be true");
    }

    /// <summary>
    /// Test 6.3.3: Test skill loading using test skill directory.
    /// Implements requirement: FR-1.1
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldLoadSkills_FromTestDirectory()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();

        // Act
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        var skillService = _serviceProvider.GetRequiredService<ISkillService>();

        // Assert - Verify skills are loaded
        var skillCount = skillService.GetSkillCount();
        skillCount.Should().BeGreaterThan(0, "should load skills from test directory");
        skillCount.Should().Be(4, "test directory contains 4 valid skills");

        var instructions = skillService.GetInstructions();
        instructions.Should().NotBeNullOrEmpty("should generate instructions");
        instructions.Should().Contain("<available_skills>", "instructions should be in XML format");
    }

    /// <summary>
    /// Test 6.3.4: Test tool registration - verify IFunctionCallback can be resolved from container.
    /// Implements requirement: FR-4.1
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldRegisterTools_AsIFunctionCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();

        // Act
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        // Assert - Verify IFunctionCallback services are registered
        var callbacks = _serviceProvider.GetServices<IFunctionCallback>().ToList();
        callbacks.Should().NotBeEmpty("should register tool callbacks");

        // Verify callbacks are AIToolCallbackAdapter instances
        callbacks.Should().AllBeOfType<AIToolCallbackAdapter>(
            "all callbacks should be AIToolCallbackAdapter instances");

        // Verify tool names
        var toolNames = callbacks.Select(c => c.Name).ToList();
        toolNames.Should().Contain("get-available-skills",
            "should include get-available-skills tool");
    }

    /// <summary>
    /// Test 6.3.5: Test hook registration - verify IAgentHook can be resolved from container.
    /// Implements requirement: FR-2.1, FR-3.1
    /// Note: This test verifies hooks are registered, but doesn't resolve them
    /// because hooks require AgentSettings which is part of the full BotSharp environment.
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldRegisterHooks_AsIAgentHook()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();

        // Act
        plugin.RegisterDI(services, configuration);

        // Assert - Verify IAgentHook services are registered in the service collection
        var hookDescriptors = services.Where(d => d.ServiceType == typeof(IAgentHook)).ToList();
        hookDescriptors.Should().NotBeEmpty("should register hooks");
        hookDescriptors.Should().HaveCountGreaterThanOrEqualTo(2, "should register at least 2 hooks");

        // Verify specific hook types are registered
        var instructionHookDescriptor = hookDescriptors.FirstOrDefault(d => 
            d.ImplementationType == typeof(AgentSkillsInstructionHook));
        instructionHookDescriptor.Should().NotBeNull("should register AgentSkillsInstructionHook");

        var functionHookDescriptor = hookDescriptors.FirstOrDefault(d => 
            d.ImplementationType == typeof(AgentSkillsFunctionHook));
        functionHookDescriptor.Should().NotBeNull("should register AgentSkillsFunctionHook");
    }

    /// <summary>
    /// Test 6.3.6: Test end-to-end workflow from plugin loading to tool invocation.
    /// Implements requirement: FR-1.1, FR-3.1, FR-4.1
    /// </summary>
    [Fact]
    public async Task EndToEnd_ShouldWorkCorrectly_FromPluginLoadToToolCall()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        // Act - Get skill service and verify it works
        var skillService = _serviceProvider.GetRequiredService<ISkillService>();
        var skillCount = skillService.GetSkillCount();
        var tools = skillService.GetTools();

        // Assert - Verify complete workflow
        skillCount.Should().BeGreaterThan(0, "should have loaded skills");
        tools.Should().NotBeEmpty("should have generated tools");

        // Verify we can get tool callbacks
        var callbacks = _serviceProvider.GetServices<IFunctionCallback>().ToList();
        callbacks.Should().NotBeEmpty("should have registered tool callbacks");

        // Verify hooks are registered in service collection
        var hookDescriptors = services.Where(d => d.ServiceType == typeof(IAgentHook)).ToList();
        hookDescriptors.Should().HaveCountGreaterThanOrEqualTo(2, "should have at least 2 hooks");

        // Verify tool callback can be invoked (basic check)
        var getAvailableSkillsTool = callbacks.FirstOrDefault(c => c.Name == "get-available-skills");
        if (getAvailableSkillsTool != null)
        {
            var message = new BotSharp.Abstraction.Conversations.Models.RoleDialogModel
            {
                FunctionName = "get-available-skills",
                FunctionArgs = "{}"
            };

            var result = await getAvailableSkillsTool.Execute(message);
            result.Should().BeTrue("tool execution should succeed");
            message.Content.Should().NotBeNullOrEmpty("tool should return content");
        }
    }

    /// <summary>
    /// Test 6.3.7: Test error scenario - skill directory does not exist.
    /// Implements requirement: FR-1.3
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldHandleNonExistentDirectory_WithoutThrowing()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfigurationWithInvalidPath();
        
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}");

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService>(new TestSettingService(nonExistentPath));

        var plugin = new AgentSkillsPlugin();

        // Act
        var act = () =>
        {
            plugin.RegisterDI(services, configuration);
            _serviceProvider = services.BuildServiceProvider();
        };

        // Assert - Should not throw exception
        act.Should().NotThrow("plugin should handle non-existent directory gracefully");

        // Verify services are still registered
        _serviceProvider.Should().NotBeNull();
        var skillService = _serviceProvider!.GetService<ISkillService>();
        skillService.Should().NotBeNull("ISkillService should still be registered");

        // Verify skill count is 0
        skillService!.GetSkillCount().Should().Be(0, "should have 0 skills when directory doesn't exist");
    }

    /// <summary>
    /// Test 6.3.8: Test singleton behavior - services should be reused.
    /// Implements requirement: NFR-1.1
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldUseSingletonServices_ForFactoryAndSkillService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();

        // Act
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        // Get services multiple times
        var factory1 = _serviceProvider.GetRequiredService<AgentSkillsFactory>();
        var factory2 = _serviceProvider.GetRequiredService<AgentSkillsFactory>();

        var skillService1 = _serviceProvider.GetRequiredService<ISkillService>();
        var skillService2 = _serviceProvider.GetRequiredService<ISkillService>();

        // Assert - Verify singleton behavior
        factory1.Should().BeSameAs(factory2, "AgentSkillsFactory should be singleton");
        skillService1.Should().BeSameAs(skillService2, "ISkillService should be singleton");
    }

    /// <summary>
    /// Test: Verify scoped behavior for tool callbacks.
    /// Implements requirement: NFR-2.1
    /// </summary>
    [Fact]
    public void RegisterDI_ShouldUseScopedServices_ForToolCallbacks()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISettingService, TestSettingService>();

        var plugin = new AgentSkillsPlugin();
        plugin.RegisterDI(services, configuration);
        _serviceProvider = services.BuildServiceProvider();

        // Act - Create two scopes and get callbacks
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var callbacks1 = scope1.ServiceProvider.GetServices<IFunctionCallback>().ToList();
        var callbacks2 = scope2.ServiceProvider.GetServices<IFunctionCallback>().ToList();

        // Assert - Verify scoped behavior (different instances per scope)
        callbacks1.Should().NotBeEmpty();
        callbacks2.Should().NotBeEmpty();
        callbacks1.Should().HaveSameCount(callbacks2);

        // Verify instances are different (scoped)
        for (int i = 0; i < callbacks1.Count && i < callbacks2.Count; i++)
        {
            callbacks1[i].Should().NotBeSameAs(callbacks2[i],
                "scoped services should create new instances per scope");
        }
    }

    #region Helper Methods

    private IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            ["AgentSkills:EnableProjectSkills"] = "true",
            ["AgentSkills:EnableUserSkills"] = "false",
            ["AgentSkills:ProjectSkillsDir"] = _testSkillsPath,
            ["AgentSkills:MaxOutputSizeBytes"] = "51200",
            ["AgentSkills:EnableReadFileTool"] = "true",
            ["AgentSkills:EnableListDirectoryTool"] = "true"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    private IConfiguration CreateTestConfigurationWithInvalidPath()
    {
        var configData = new Dictionary<string, string>
        {
            ["AgentSkills:EnableProjectSkills"] = "true",
            ["AgentSkills:EnableUserSkills"] = "false",
            ["AgentSkills:ProjectSkillsDir"] = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}"),
            ["AgentSkills:MaxOutputSizeBytes"] = "51200"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    #endregion

    #region Test Helper Classes

    /// <summary>
    /// Test implementation of ISettingService for integration tests.
    /// </summary>
    private class TestSettingService : ISettingService
    {
        private readonly string? _customSkillsPath;

        public TestSettingService(string? customSkillsPath = null)
        {
            _customSkillsPath = customSkillsPath;
        }

        public T Bind<T>(string key) where T : new()
        {
            if (typeof(T) == typeof(AgentSkillsSettings))
            {
                var agentSkillsSettings = new AgentSkillsSettings();
                var testSkillsPath = _customSkillsPath ?? 
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test-skills");
                
                agentSkillsSettings.EnableProjectSkills = true;
                agentSkillsSettings.EnableUserSkills = false;
                agentSkillsSettings.ProjectSkillsDir = testSkillsPath;
                agentSkillsSettings.MaxOutputSizeBytes = 51200;
                agentSkillsSettings.EnableReadFileTool = true;
                agentSkillsSettings.EnableListDirectoryTool = true;
                
                return (T)(object)agentSkillsSettings;
            }
            
            return new T();
        }

        public Task<object> GetDetail(string settingName, bool mask = false)
        {
            return Task.FromResult<object>(new { });
        }
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
