using OpenSandbox;
using OpenSandbox.CodeInterpreter;
using OpenSandbox.CodeInterpreter.Models;
using OpenSandbox.Config;

namespace BotSharp.Plugin.CodeAct.OpenSandbox;

public class OpenSandboxHttpCodeClient : IOpenSandboxCodeClient
{
    private const string SessionStateKey = "_opensandbox_sdk_state";
    private static readonly string[] DefaultEntrypoint = { "/opt/opensandbox/code-interpreter.sh" };

    private readonly CodeActSettings _settings;
    private readonly ConnectionConfig _connectionConfig;

    public OpenSandboxHttpCodeClient(CodeActSettings settings)
    {
        _settings = settings;
        _connectionConfig = CreateConnectionConfig(settings);
    }

    public async Task<OpenSandboxSession> CreateSessionAsync(OpenSandboxSessionOptions options, CancellationToken cancellationToken)
    {
        var image = string.IsNullOrWhiteSpace(_settings.OpenSandbox.RuntimeImage)
            ? "opensandbox/code-interpreter:v1.0.2"
            : _settings.OpenSandbox.RuntimeImage;
        IReadOnlyList<string> entrypoint = (_settings.OpenSandbox.Entrypoint?.Count ?? 0) > 0
            ? _settings.OpenSandbox.Entrypoint
            : DefaultEntrypoint;

        var sandbox = await Sandbox.CreateAsync(new SandboxCreateOptions
        {
            ConnectionConfig = _connectionConfig,
            Image = image,
            Entrypoint = entrypoint,
            TimeoutSeconds = options.TtlSeconds,
            Resource = BuildResource(options),
            Metadata = ToStringDictionary(options.Metadata)
        }, cancellationToken);

        var interpreter = await CodeInterpreter.CreateAsync(sandbox, cancellationToken: cancellationToken);
        var context = await interpreter.Codes.CreateContextAsync(NormalizeLanguage(options.Language), cancellationToken);

        var session = new OpenSandboxSession
        {
            Id = sandbox.Id,
            ContextId = context.Id,
            Metadata = new Dictionary<string, object?> { ["source"] = "alibaba-sdk" }
        };

        SetSessionState(session, new OpenSandboxSdkSessionState(sandbox, interpreter));
        return session;
    }

    public async IAsyncEnumerable<OpenSandboxCodeEvent> RunCodeAsync(OpenSandboxRunCodeRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var state = await GetOrCreateSessionStateAsync(request.Session, cancellationToken);
        var language = NormalizeLanguage(request.Language);

        if (string.IsNullOrWhiteSpace(request.Session.ContextId))
        {
            var newContext = await state.Interpreter.Codes.CreateContextAsync(language, cancellationToken);
            request.Session.ContextId = newContext.Id;
        }

        var streamRequest = new RunCodeRequest
        {
            Code = request.Code,
            Context = new CodeContext
            {
                Id = request.Session.ContextId,
                Language = language
            }
        };

        await foreach (var ev in state.Interpreter.Codes.RunStreamAsync(streamRequest, cancellationToken).WithCancellation(cancellationToken))
        {
            var mapped = MapEvent(ev);
            if (mapped != null)
            {
                yield return mapped;
            }
        }

        yield return new OpenSandboxCodeEvent
        {
            Type = OpenSandboxCodeEventTypes.Completed,
            Content = "OpenSandbox code execution completed.",
            Metadata = new Dictionary<string, object?>()
        };
    }

    public async Task DestroySessionAsync(OpenSandboxSession session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session.Id))
        {
            return;
        }

        var state = await GetOrCreateSessionStateAsync(session, cancellationToken);
        try
        {
            await state.Sandbox.KillAsync(cancellationToken);
        }
        finally
        {
            await state.Sandbox.DisposeAsync();
            session.Metadata.Remove(SessionStateKey);
        }
    }

    private async Task<OpenSandboxSdkSessionState> GetOrCreateSessionStateAsync(OpenSandboxSession session, CancellationToken cancellationToken)
    {
        if (TryGetSessionState(session, out var existing))
        {
            return existing;
        }

        var sandbox = await Sandbox.ConnectAsync(new SandboxConnectOptions
        {
            ConnectionConfig = _connectionConfig,
            SandboxId = session.Id
        }, cancellationToken);

        var interpreter = await CodeInterpreter.CreateAsync(sandbox, cancellationToken: cancellationToken);
        var state = new OpenSandboxSdkSessionState(sandbox, interpreter);
        SetSessionState(session, state);
        return state;
    }

    private static OpenSandboxCodeEvent? MapEvent(global::OpenSandbox.Models.ServerStreamEvent ev)
    {
        var type = NormalizeType(ev.Type);
        if (type == OpenSandboxCodeEventTypes.Unknown)
        {
            return null;
        }

        var content = ev.Text;
        if (string.IsNullOrWhiteSpace(content) && ev.Results != null)
        {
            content = ReadResultText(ev.Results);
        }

        var errorMessage = ev.Error == null ? null : ReadErrorMessage(ev.Error);
        return new OpenSandboxCodeEvent
        {
            Type = type,
            Content = content,
            ErrorCode = errorMessage == null ? null : "opensandbox.execution_error",
            ErrorMessage = errorMessage,
            Metadata = new Dictionary<string, object?>
            {
                ["event_type"] = ev.Type,
                ["execution_count"] = ev.ExecutionCount,
                ["timestamp"] = ev.Timestamp
            }
        };
    }

    private static string? ReadResultText(Dictionary<string, object>? results)
    {
        if (results == null || results.Count == 0)
        {
            return null;
        }

        if (results.TryGetValue("text/plain", out var textPlain) && textPlain != null)
        {
            return textPlain.ToString();
        }

        return results.Values.FirstOrDefault(v => v != null)?.ToString();
    }

    private static string? ReadErrorMessage(Dictionary<string, object> error)
    {
        if (error.TryGetValue("message", out var message) && message != null)
        {
            return message.ToString();
        }

        if (error.TryGetValue("value", out var value) && value != null)
        {
            return value.ToString();
        }

        return error.Values.FirstOrDefault(v => v != null)?.ToString();
    }

    private static string NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return OpenSandboxCodeEventTypes.Unknown;
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "stdout" => OpenSandboxCodeEventTypes.Stdout,
            "stderr" => OpenSandboxCodeEventTypes.Stderr,
            "error" => OpenSandboxCodeEventTypes.Error,
            "result" or "completed" or "complete" or "done" => OpenSandboxCodeEventTypes.Completed,
            _ => OpenSandboxCodeEventTypes.Unknown
        };
    }

    private static string NormalizeLanguage(string? language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            "python" or "py" => SupportedLanguage.Python,
            "javascript" or "js" => SupportedLanguage.JavaScript,
            "typescript" or "ts" => SupportedLanguage.TypeScript,
            "go" or "golang" => SupportedLanguage.Go,
            "java" => SupportedLanguage.Java,
            "bash" or "sh" or "shell" => SupportedLanguage.Bash,
            _ => SupportedLanguage.Python
        };
    }

    private static void SetSessionState(OpenSandboxSession session, OpenSandboxSdkSessionState state)
    {
        session.Metadata[SessionStateKey] = state;
    }

    private static bool TryGetSessionState(OpenSandboxSession session, out OpenSandboxSdkSessionState state)
    {
        if (session.Metadata.TryGetValue(SessionStateKey, out var value) && value is OpenSandboxSdkSessionState typed)
        {
            state = typed;
            return true;
        }

        state = null!;
        return false;
    }

    private static Dictionary<string, string> BuildResource(OpenSandboxSessionOptions options)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (options.CpuLimit.HasValue)
        {
            result["cpu"] = options.CpuLimit.Value.ToString();
        }

        if (options.MemoryMb.HasValue)
        {
            result["memory_mb"] = options.MemoryMb.Value.ToString();
        }

        return result;
    }

    private static Dictionary<string, string> ToStringDictionary(Dictionary<string, object?> source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in source)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null)
            {
                result[key] = value.ToString() ?? string.Empty;
            }
        }

        return result;
    }

    private static ConnectionConfig CreateConnectionConfig(CodeActSettings settings)
    {
        var options = new ConnectionConfigOptions();
        var controlPlaneUrl = settings.OpenSandbox.ControlPlaneBaseUrl;

        if (Uri.TryCreate(controlPlaneUrl, UriKind.Absolute, out var uri))
        {
            options.Domain = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            options.Protocol = uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                ? ConnectionProtocol.Http
                : ConnectionProtocol.Https;
        }
        else if (!string.IsNullOrWhiteSpace(controlPlaneUrl))
        {
            options.Domain = controlPlaneUrl;
        }

        if (!string.IsNullOrWhiteSpace(settings.OpenSandbox.ApiKey))
        {
            options.ApiKey = settings.OpenSandbox.ApiKey;
        }

        return new ConnectionConfig(options);
    }

    private sealed class OpenSandboxSdkSessionState
    {
        public OpenSandboxSdkSessionState(Sandbox sandbox, CodeInterpreter interpreter)
        {
            Sandbox = sandbox;
            Interpreter = interpreter;
        }

        public Sandbox Sandbox { get; }

        public CodeInterpreter Interpreter { get; }
    }
}
