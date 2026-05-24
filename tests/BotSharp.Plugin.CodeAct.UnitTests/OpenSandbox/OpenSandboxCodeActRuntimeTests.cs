namespace BotSharp.Plugin.CodeAct.UnitTests.OpenSandbox;

public class OpenSandboxCodeActRuntimeTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess_WithStreamedOutput()
    {
        var client = new FakeOpenSandboxCodeClient
        {
            Events =
            [
                new() { Type = OpenSandboxCodeEventTypes.Stdout, Content = "hello" },
                new() { Type = OpenSandboxCodeEventTypes.Completed }
            ]
        };
        var runtime = CreateRuntime(client);

        var result = await runtime.ExecuteAsync(new CodeActRequest { Language = "python", Code = "print('hello')", ReadOnly = true });

        Assert.True(result.Success);
        Assert.Equal("hello", result.Stdout);
        Assert.Equal("sandbox-1", result.Metadata["sandbox_id"]);
        Assert.Equal(1, client.CreateCalls);
        Assert.Equal(1, client.DestroyCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_ForErrorEvent_WithPartialOutput()
    {
        var client = new FakeOpenSandboxCodeClient
        {
            Events =
            [
                new() { Type = OpenSandboxCodeEventTypes.Stdout, Content = "before error" },
                new() { Type = OpenSandboxCodeEventTypes.Error, ErrorCode = "python.error", ErrorMessage = "boom" }
            ]
        };
        var runtime = CreateRuntime(client);

        var result = await runtime.ExecuteAsync(new CodeActRequest { Language = "python", Code = "raise Exception()", ReadOnly = true });

        Assert.False(result.Success);
        Assert.Equal("python.error", result.ErrorCode);
        Assert.Equal("before error", result.Stdout);
        Assert.Equal("boom", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_TruncatesStdout_AndAddsTrace()
    {
        var client = new FakeOpenSandboxCodeClient
        {
            Events =
            [
                new() { Type = OpenSandboxCodeEventTypes.Stdout, Content = "abcdef" },
                new() { Type = OpenSandboxCodeEventTypes.Completed }
            ]
        };
        var runtime = CreateRuntime(client, settings => settings.OpenSandbox.MaxStdoutChars = 3);

        var result = await runtime.ExecuteAsync(new CodeActRequest { Language = "python", Code = "print('abcdef')", ReadOnly = true });

        Assert.True(result.Success);
        Assert.Equal("abc", result.Stdout);
        Assert.Contains(result.Trace, x => x.Event == "stdout.truncated");
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfiguredSandbox_WithoutCreateOrDestroy()
    {
        var client = new FakeOpenSandboxCodeClient
        {
            Events = [new() { Type = OpenSandboxCodeEventTypes.Completed }]
        };
        var runtime = CreateRuntime(client, settings =>
        {
            settings.OpenSandbox.CreateSandboxPerExecution = false;
            settings.OpenSandbox.SandboxId = "configured-sandbox";
        });

        var result = await runtime.ExecuteAsync(new CodeActRequest { Language = "python", Code = "x = 1", ReadOnly = true });

        Assert.True(result.Success);
        Assert.Equal(0, client.CreateCalls);
        Assert.Equal(0, client.DestroyCalls);
        Assert.Equal("configured-sandbox", client.LastRunRequest?.Session.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTimeout_WhenClientIsCanceledByRuntimeTimeout()
    {
        var client = new FakeOpenSandboxCodeClient { DelayBeforeEvents = TimeSpan.FromSeconds(5) };
        var runtime = CreateRuntime(client, settings => settings.ExecutionTimeoutSeconds = 1);

        var result = await runtime.ExecuteAsync(new CodeActRequest { Language = "python", Code = "while True: pass", ReadOnly = true });

        Assert.False(result.Success);
        Assert.Equal("opensandbox.timeout", result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_Denies_NonReadOnlyRequest()
    {
        var runtime = CreateRuntime(new FakeOpenSandboxCodeClient());

        var result = await runtime.ExecuteAsync(new CodeActRequest { Language = "python", Code = "print('hello')", ReadOnly = false });

        Assert.False(result.Success);
        Assert.Equal("codeact.read_only_required", result.ErrorCode);
    }

    private static OpenSandboxCodeActRuntime CreateRuntime(FakeOpenSandboxCodeClient client, Action<CodeActSettings>? configure = null)
    {
        var settings = new CodeActSettings
        {
            Runtime = "opensandbox",
            ExecutionTimeoutSeconds = 10,
            OpenSandbox = new OpenSandboxCodeActSettings
            {
                MaxStdoutChars = 20000,
                MaxStderrChars = 12000,
                MaxTraceEvents = 200
            }
        };
        configure?.Invoke(settings);
        return new OpenSandboxCodeActRuntime(settings, client);
    }

    private sealed class FakeOpenSandboxCodeClient : IOpenSandboxCodeClient
    {
        public List<OpenSandboxCodeEvent> Events { get; set; } = [];
        public TimeSpan DelayBeforeEvents { get; set; }
        public int CreateCalls { get; private set; }
        public int DestroyCalls { get; private set; }
        public OpenSandboxRunCodeRequest? LastRunRequest { get; private set; }

        public Task<OpenSandboxSession> CreateSessionAsync(OpenSandboxSessionOptions options, CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(new OpenSandboxSession { Id = "sandbox-1", ContextId = "context-1" });
        }

        public async IAsyncEnumerable<OpenSandboxCodeEvent> RunCodeAsync(OpenSandboxRunCodeRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRunRequest = request;
            if (DelayBeforeEvents > TimeSpan.Zero)
            {
                await Task.Delay(DelayBeforeEvents, cancellationToken);
            }

            foreach (var codeEvent in Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return codeEvent;
            }
        }

        public Task DestroySessionAsync(OpenSandboxSession session, CancellationToken cancellationToken)
        {
            DestroyCalls++;
            return Task.CompletedTask;
        }
    }
}
