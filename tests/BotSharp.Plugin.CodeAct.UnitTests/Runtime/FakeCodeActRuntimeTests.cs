namespace BotSharp.Plugin.CodeAct.UnitTests.Runtime;

public class FakeCodeActRuntimeTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_HelloWorld_ForSafeCode()
    {
        var runtime = new FakeCodeActRuntime();

        var result = await runtime.ExecuteAsync(new CodeActRequest
        {
            Language = "python",
            Code = "print('hello')",
            ReadOnly = true
        });

        Assert.True(result.Success);
        Assert.Contains("hello", result.Content, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.Trace);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_Denies_NonReadOnly_Request()
    {
        var runtime = new FakeCodeActRuntime();

        var result = await runtime.ExecuteAsync(new CodeActRequest
        {
            Language = "python",
            Code = "print('hello')",
            ReadOnly = false
        });

        Assert.False(result.Success);
        Assert.Equal("codeact.read_only_required", result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_Denies_MutationLikeCode()
    {
        var runtime = new FakeCodeActRuntime();

        var result = await runtime.ExecuteAsync(new CodeActRequest
        {
            Language = "python",
            Code = "delete_database()",
            ReadOnly = true
        });

        Assert.False(result.Success);
        Assert.Equal("codeact.mutation_denied", result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_Denies_UnsupportedLanguage()
    {
        var runtime = new FakeCodeActRuntime();

        var result = await runtime.ExecuteAsync(new CodeActRequest
        {
            Language = "bash",
            Code = "echo hello",
            ReadOnly = true
        });

        Assert.False(result.Success);
        Assert.Equal("codeact.unsupported_language", result.ErrorCode);
    }
}
