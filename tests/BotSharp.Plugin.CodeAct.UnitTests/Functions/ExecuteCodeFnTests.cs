namespace BotSharp.Plugin.CodeAct.UnitTests.Functions;

public class ExecuteCodeFnTests
{
    [Fact]
    public async Task Execute_Maps_RuntimeSuccess_ToMessage()
    {
        var fn = new ExecuteCodeFn(new FakeCodeActRuntime(), new CodeActSettings { ReadOnlyPilot = true });
        var message = new RoleDialogModel("function", string.Empty)
        {
            CurrentAgentId = "agent-1",
            SenderId = "user-1",
            FunctionArgs = JsonSerializer.Serialize(new ExecuteCodeArgs
            {
                Language = "python",
                Code = "print('hello')",
                ReadOnly = true
            })
        };

        var success = await fn.Execute(message);

        Assert.True(success);
        Assert.Contains("hello", message.Content, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<CodeActResult>(message.Data);
        Assert.True(message.StopCompletion);
    }

    [Fact]
    public async Task Execute_ReturnsFailure_ForInvalidJson()
    {
        var fn = new ExecuteCodeFn(new FakeCodeActRuntime(), new CodeActSettings { ReadOnlyPilot = true });
        var message = new RoleDialogModel("function", string.Empty) { FunctionArgs = "{" };

        var success = await fn.Execute(message);

        Assert.False(success);
        var result = Assert.IsType<CodeActResult>(message.Data);
        Assert.Equal("codeact.invalid_arguments", result.ErrorCode);
        Assert.True(message.StopCompletion);
    }

    [Fact]
    public async Task Execute_ReturnsFailure_ForEmptyCode()
    {
        var fn = new ExecuteCodeFn(new FakeCodeActRuntime(), new CodeActSettings { ReadOnlyPilot = true });
        var message = new RoleDialogModel("function", string.Empty)
        {
            FunctionArgs = JsonSerializer.Serialize(new ExecuteCodeArgs { Code = "" })
        };

        var success = await fn.Execute(message);

        Assert.False(success);
        var result = Assert.IsType<CodeActResult>(message.Data);
        Assert.Equal("codeact.empty_code", result.ErrorCode);
    }

    [Fact]
    public async Task Execute_Maps_RuntimeDenial_ToMessageData()
    {
        var fn = new ExecuteCodeFn(new FakeCodeActRuntime(), new CodeActSettings { ReadOnlyPilot = true });
        var message = new RoleDialogModel("function", string.Empty)
        {
            FunctionArgs = JsonSerializer.Serialize(new ExecuteCodeArgs
            {
                Language = "python",
                Code = "delete_database()",
                ReadOnly = true
            })
        };

        var success = await fn.Execute(message);

        Assert.False(success);
        var result = Assert.IsType<CodeActResult>(message.Data);
        Assert.Equal("codeact.mutation_denied", result.ErrorCode);
        Assert.Equal(result.Content, message.Content);
    }
}
