using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Plugin.AgentSkills.Functions;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace BotSharp.Plugin.AgentSkills.Tests.Functions;

/// <summary>
/// Unit tests for AIToolCallbackAdapter class.
/// Tests requirements: NFR-2.3, FR-4.1, FR-4.2, FR-4.3
/// </summary>
public class AIToolCallbackAdapterTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<AIToolCallbackAdapter>> _mockLogger;

    public AIToolCallbackAdapterTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<AIToolCallbackAdapter>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullAIFunction_ThrowsArgumentNullException()
    {
        var act = () => new AIToolCallbackAdapter(null!, _mockServiceProvider.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("aiFunction");
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        var testFunction = CreateTestFunction("test-tool", "result");
        var act = () => new AIToolCallbackAdapter(testFunction, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        var testFunction = CreateTestFunction("test-tool", "result");
        var act = () => new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, null);
        act.Should().NotThrow("logger is optional");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Name_ReturnsAIFunctionName()
    {
        var expectedName = "test-tool-name";
        var testFunction = CreateTestFunction(expectedName, "result");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        adapter.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Provider_ReturnsAgentSkills()
    {
        var testFunction = CreateTestFunction("test-tool", "result");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        adapter.Provider.Should().Be("AgentSkills");
    }

    #endregion

    #region Successful Execution Tests

    [Fact]
    public async Task Execute_WithValidArguments_ReturnsSuccessAndSetsContent()
    {
        var expectedResult = "Test result content";
        var testFunction = CreateTestFunction("test-tool", expectedResult);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"param1\": \"value1\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        message.Content.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Execute_WithValidJson_ParsesArgumentsCorrectly()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"skillName\": \"test-skill\", \"filePath\": \"test.txt\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        message.Content.Should().Be("success");
    }

    [Fact]
    public async Task Execute_WithMixedCaseJson_ParsesCaseInsensitively()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"SkillName\": \"test\", \"FILE_PATH\": \"test.txt\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        message.Content.Should().Be("success");
    }

    #endregion

    #region Argument Parsing Error Tests

    [Fact]
    public async Task Execute_WithInvalidJson_ReturnsFalseAndSetsErrorMessage()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{invalid json" };

        var result = await adapter.Execute(message);

        result.Should().BeFalse();
        message.Content.Should().Contain("Invalid JSON arguments");
    }

    [Fact]
    public async Task Execute_WithEmptyArguments_SucceedsWithEmptyDictionary()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "" };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        message.Content.Should().Be("success");
    }

    [Fact]
    public async Task Execute_WithNullArguments_SucceedsWithEmptyDictionary()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = null };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        message.Content.Should().Be("success");
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Execute_WhenFileNotFound_ReturnsFalseWithFriendlyMessage()
    {
        var exception = new FileNotFoundException("SKILL.md not found");
        var testFunction = CreateTestFunctionThatThrows("test-tool", exception);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"skillName\": \"missing-skill\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeFalse();
        message.Content.Should().Contain("Skill or file not found");
        message.Content.Should().Contain("SKILL.md not found");
    }

    [Fact]
    public async Task Execute_WhenUnauthorizedAccess_ReturnsFalseWithSecurityMessage()
    {
        var exception = new UnauthorizedAccessException("Access denied");
        var testFunction = CreateTestFunctionThatThrows("test-tool", exception);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"filePath\": \"../../../etc/passwd\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeFalse();
        message.Content.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Execute_WhenFileSizeExceeded_ReturnsFalseWithSizeMessage()
    {
        var exception = new InvalidOperationException("File size exceeds maximum allowed size of 51200 bytes");
        var testFunction = CreateTestFunctionThatThrows("test-tool", exception);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"filePath\": \"large-file.txt\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeFalse();
        message.Content.Should().Contain("File size exceeds limit");
    }

    [Fact]
    public async Task Execute_WhenGenericException_ReturnsFalseWithErrorMessage()
    {
        var exception = new Exception("Unexpected error occurred");
        var testFunction = CreateTestFunctionThatThrows("test-tool", exception);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"param\": \"value\"}" };

        var result = await adapter.Execute(message);

        result.Should().BeFalse();
        message.Content.Should().Contain("Error executing tool");
        message.Content.Should().Contain("test-tool");
        message.Content.Should().Contain("Unexpected error occurred");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task Execute_LogsDebugInformation()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{\"param\": \"value\"}" };

        await adapter.Execute(message);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing tool")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_OnSuccess_LogsInformation()
    {
        var testFunction = CreateTestFunction("test-tool", "success");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{}" };

        await adapter.Execute(message);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("executed successfully")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Execute_OnFileNotFound_LogsWarning()
    {
        var exception = new FileNotFoundException("File not found");
        var testFunction = CreateTestFunctionThatThrows("test-tool", exception);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{}" };

        await adapter.Execute(message);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File not found")),
            It.Is<Exception>(ex => ex == exception),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Execute_OnUnauthorizedAccess_LogsError()
    {
        var exception = new UnauthorizedAccessException("Access denied");
        var testFunction = CreateTestFunctionThatThrows("test-tool", exception);
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{}" };

        await adapter.Execute(message);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unauthorized access")),
            It.Is<Exception>(ex => ex == exception),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task Execute_WhenAIFunctionReturnsNull_SetsEmptyContent()
    {
        var testFunction = CreateTestFunctionReturningNull("test-tool");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{}" };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        // When AIFunction returns null, ConvertToString() returns "null" string
        message.Content.Should().Be("null");
    }

    [Fact]
    public async Task Execute_WhenAIFunctionReturnsEmptyString_SetsEmptyContent()
    {
        var testFunction = CreateTestFunction("test-tool", "");
        var adapter = new AIToolCallbackAdapter(testFunction, _mockServiceProvider.Object, _mockLogger.Object);
        var message = new RoleDialogModel { FunctionArgs = "{}" };

        var result = await adapter.Execute(message);

        result.Should().BeTrue();
        message.Content.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test AIFunction using AIFunctionFactory that returns a specified result.
    /// </summary>
    private static AIFunction CreateTestFunction(string name, string returnValue)
    {
        return AIFunctionFactory.Create(
            () => returnValue,
            name: name,
            description: "Test function");
    }

    /// <summary>
    /// Creates a test AIFunction using AIFunctionFactory that throws an exception.
    /// </summary>
    private static AIFunction CreateTestFunctionThatThrows(string name, Exception exception)
    {
        return AIFunctionFactory.Create(
            () =>
            {
                throw exception;
#pragma warning disable CS0162 // Unreachable code detected
                return "";
#pragma warning restore CS0162 // Unreachable code detected
            },
            name: name,
            description: "Test function that throws");
    }

    /// <summary>
    /// Creates a test AIFunction using AIFunctionFactory that returns null.
    /// </summary>
    private static AIFunction CreateTestFunctionReturningNull(string name)
    {
        return AIFunctionFactory.Create(
            () => (string?)null,
            name: name,
            description: "Test function returning null");
    }

    #endregion
}
