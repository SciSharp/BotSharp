namespace BotSharp.Plugin.CodeAct.OpenSandbox;

public class OpenSandboxCodeActRuntime : ICodeActRuntime
{
    private readonly CodeActSettings _settings;
    private readonly IOpenSandboxCodeClient _client;

    public string Name => "opensandbox";

    public OpenSandboxCodeActRuntime(CodeActSettings settings, IOpenSandboxCodeClient client)
    {
        _settings = settings;
        _client = client;
    }

    public async Task<CodeActResult> ExecuteAsync(CodeActRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var trace = new List<CodeActTrace>();
        var stdout = new BoundedTextBuffer(Math.Max(0, _settings.OpenSandbox.MaxStdoutChars));
        var stderr = new BoundedTextBuffer(Math.Max(0, _settings.OpenSandbox.MaxStderrChars));
        var language = string.IsNullOrWhiteSpace(request.Language) ? _settings.OpenSandbox.Language : request.Language;
        var timeoutSeconds = Math.Max(1, _settings.ExecutionTimeoutSeconds);
        OpenSandboxSession? session = null;
        var ownsSession = _settings.OpenSandbox.CreateSandboxPerExecution || string.IsNullOrWhiteSpace(_settings.OpenSandbox.SandboxId);

        AddTrace(trace, "runtime.started", "OpenSandbox CodeAct runtime accepted the request.", new Dictionary<string, object?>
        {
            ["request_id"] = request.RequestId,
            ["language"] = language,
            ["read_only"] = request.ReadOnly
        });

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Complete(Failure("codeact.empty_code", "OpenSandbox CodeAct runtime requires non-empty code.", trace), stopwatch);
        }

        if (!request.ReadOnly)
        {
            return Complete(Failure("codeact.read_only_required", "CodeAct pilot only supports read-only OpenSandbox execution.", trace), stopwatch);
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            session = ownsSession
                ? await _client.CreateSessionAsync(CreateSessionOptions(language, request), linked.Token)
                : ExistingSession();

            AddTrace(trace, "sandbox.ready", "OpenSandbox session is ready.", new Dictionary<string, object?>
            {
                ["sandbox_id"] = session.Id,
                ["created"] = ownsSession
            });

            var runRequest = new OpenSandboxRunCodeRequest
            {
                Session = session,
                Language = language,
                Code = request.Code,
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                Metadata = request.Metadata
            };

            var completed = false;
            await foreach (var codeEvent in _client.RunCodeAsync(runRequest, linked.Token).WithCancellation(linked.Token))
            {
                switch (codeEvent.Type)
                {
                    case OpenSandboxCodeEventTypes.Stdout:
                        Append(stdout, codeEvent.Content, trace, "stdout.truncated", "OpenSandbox stdout was truncated.");
                        AddTrace(trace, "runtime.stdout", codeEvent.Content, codeEvent.Metadata);
                        break;

                    case OpenSandboxCodeEventTypes.Stderr:
                        Append(stderr, codeEvent.Content, trace, "stderr.truncated", "OpenSandbox stderr was truncated.");
                        AddTrace(trace, "runtime.stderr", codeEvent.Content, codeEvent.Metadata);
                        break;

                    case OpenSandboxCodeEventTypes.Error:
                        AddTrace(trace, "runtime.error", codeEvent.ErrorMessage ?? codeEvent.Content, codeEvent.Metadata);
                        return Complete(new CodeActResult
                        {
                            Success = false,
                            Content = codeEvent.ErrorMessage ?? codeEvent.Content ?? "OpenSandbox code execution failed.",
                            Stdout = stdout.ToString(),
                            Stderr = stderr.ToString(),
                            ErrorCode = string.IsNullOrWhiteSpace(codeEvent.ErrorCode) ? "opensandbox.execution_error" : codeEvent.ErrorCode,
                            ErrorMessage = codeEvent.ErrorMessage ?? codeEvent.Content,
                            Trace = trace,
                            Metadata = ResultMetadata(session, ownsSession)
                        }, stopwatch);

                    case OpenSandboxCodeEventTypes.Completed:
                        completed = true;
                        AddTrace(trace, "runtime.completed_event", codeEvent.Content, codeEvent.Metadata);
                        break;

                    default:
                        AddTrace(trace, "runtime.event", codeEvent.Content, codeEvent.Metadata);
                        break;
                }
            }

            var content = stdout.Length > 0 ? stdout.ToString() : stderr.ToString();
            var success = completed || stderr.Length == 0;
            return Complete(new CodeActResult
            {
                Success = success,
                Content = string.IsNullOrWhiteSpace(content) ? (success ? "OpenSandbox code execution completed." : "OpenSandbox code execution failed.") : content,
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString(),
                ErrorCode = success ? null : "opensandbox.stderr",
                ErrorMessage = success ? null : stderr.ToString(),
                Trace = trace,
                Metadata = ResultMetadata(session, ownsSession)
            }, stopwatch);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AddTrace(trace, "runtime.timeout", "OpenSandbox execution timed out.");
            return Complete(new CodeActResult
            {
                Success = false,
                Content = "OpenSandbox code execution timed out.",
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString(),
                ErrorCode = "opensandbox.timeout",
                ErrorMessage = "OpenSandbox code execution timed out.",
                Trace = trace,
                Metadata = session == null ? [] : ResultMetadata(session, ownsSession)
            }, stopwatch);
        }
        catch (Exception ex)
        {
            AddTrace(trace, "runtime.exception", ex.Message, new Dictionary<string, object?> { ["exception_type"] = ex.GetType().Name });
            return Complete(new CodeActResult
            {
                Success = false,
                Content = $"OpenSandbox code execution failed: {ex.Message}",
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString(),
                ErrorCode = "opensandbox.runtime_error",
                ErrorMessage = ex.Message,
                Trace = trace,
                Metadata = session == null ? [] : ResultMetadata(session, ownsSession)
            }, stopwatch);
        }
        finally
        {
            if (ownsSession && session != null)
            {
                try
                {
                    await _client.DestroySessionAsync(session, CancellationToken.None);
                    AddTrace(trace, "sandbox.destroyed", "OpenSandbox session was destroyed.", new Dictionary<string, object?> { ["sandbox_id"] = session.Id });
                }
                catch (Exception ex)
                {
                    AddTrace(trace, "sandbox.destroy_failed", ex.Message, new Dictionary<string, object?> { ["sandbox_id"] = session.Id });
                }
            }
        }
    }

    private OpenSandboxSessionOptions CreateSessionOptions(string language, CodeActRequest request)
    {
        return new OpenSandboxSessionOptions
        {
            Language = language,
            TtlSeconds = _settings.OpenSandbox.SandboxTtlSeconds,
            CpuLimit = _settings.OpenSandbox.CpuLimit,
            MemoryMb = _settings.OpenSandbox.MemoryMb,
            EnableNetwork = _settings.OpenSandbox.EnableNetwork,
            AllowedHosts = _settings.OpenSandbox.AllowedHosts,
            Metadata = new Dictionary<string, object?>
            {
                ["request_id"] = request.RequestId,
                ["conversation_id"] = request.ConversationId,
                ["agent_id"] = request.AgentId,
                ["user_id"] = request.UserId
            }
        };
    }

    private OpenSandboxSession ExistingSession()
    {
        return new OpenSandboxSession
        {
            Id = _settings.OpenSandbox.SandboxId,
            DataPlaneBaseUrl = TryCreateUri(_settings.OpenSandbox.DataPlaneBaseUrl),
            Metadata = new Dictionary<string, object?> { ["configured"] = true }
        };
    }

    private static CodeActResult Failure(string errorCode, string message, List<CodeActTrace> trace)
    {
        return new CodeActResult
        {
            Success = false,
            Content = message,
            ErrorCode = errorCode,
            ErrorMessage = message,
            Trace = trace
        };
    }

    private CodeActResult Complete(CodeActResult result, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        AddTrace(result.Trace, "runtime.completed", result.Success ? "OpenSandbox CodeAct runtime completed successfully." : "OpenSandbox CodeAct runtime completed with an error.", new Dictionary<string, object?>
        {
            ["success"] = result.Success,
            ["error_code"] = result.ErrorCode,
            ["duration_ms"] = result.Duration.TotalMilliseconds
        });
        return result;
    }

    private void AddTrace(List<CodeActTrace> trace, string @event, string? message, Dictionary<string, object?>? attributes = null)
    {
        var maxTraceEvents = Math.Max(1, _settings.OpenSandbox.MaxTraceEvents);
        if (trace.Count >= maxTraceEvents)
        {
            return;
        }

        trace.Add(new CodeActTrace
        {
            Event = @event,
            Component = Name,
            Message = message,
            Attributes = attributes ?? []
        });
    }

    private void Append(BoundedTextBuffer buffer, string? content, List<CodeActTrace> trace, string traceEvent, string traceMessage)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        var wasTruncated = buffer.Truncated;
        buffer.Append(content);
        if (!wasTruncated && buffer.Truncated)
        {
            AddTrace(trace, traceEvent, traceMessage);
        }
    }

    private static Dictionary<string, object?> ResultMetadata(OpenSandboxSession session, bool created)
    {
        return new Dictionary<string, object?>
        {
            ["runtime"] = "opensandbox",
            ["sandbox_id"] = session.Id,
            ["context_id"] = session.ContextId,
            ["sandbox_created"] = created
        };
    }

    private static Uri? TryCreateUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private sealed class BoundedTextBuffer
    {
        private readonly int _maxChars;
        private readonly System.Text.StringBuilder _builder = new();

        public int Length => _builder.Length;
        public bool Truncated { get; private set; }

        public BoundedTextBuffer(int maxChars)
        {
            _maxChars = maxChars;
        }

        public void Append(string value)
        {
            if (_maxChars == 0)
            {
                Truncated = true;
                return;
            }

            var remaining = _maxChars - _builder.Length;
            if (remaining <= 0)
            {
                Truncated = true;
                return;
            }

            if (value.Length <= remaining)
            {
                _builder.Append(value);
                return;
            }

            _builder.Append(value.AsSpan(0, remaining));
            Truncated = true;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
