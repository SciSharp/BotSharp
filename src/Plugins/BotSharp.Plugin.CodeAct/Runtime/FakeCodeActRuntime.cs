namespace BotSharp.Plugin.CodeAct.Runtime;

public class FakeCodeActRuntime : ICodeActRuntime
{
    private static readonly string[] MutationSignals =
    [
        "write",
        "delete",
        "remove",
        "drop",
        "update",
        "insert",
        "rm ",
        "curl -x post",
        "curl -xpost"
    ];

    public string Name => "fake";

    public Task<CodeActResult> ExecuteAsync(CodeActRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var trace = new List<CodeActTrace>
        {
            new()
            {
                Event = "runtime.started",
                Component = Name,
                Message = "Fake CodeAct runtime accepted the request.",
                Attributes = new Dictionary<string, object?>
                {
                    ["request_id"] = request.RequestId,
                    ["language"] = request.Language,
                    ["read_only"] = request.ReadOnly
                }
            }
        };

        CodeActResult result;
        if (!request.ReadOnly)
        {
            result = Denied("codeact.read_only_required", "CodeAct pilot only supports read-only execution.", trace);
        }
        else if (!IsSupportedLanguage(request.Language))
        {
            result = Denied("codeact.unsupported_language", $"Language '{request.Language}' is not supported by the fake CodeAct runtime.", trace);
        }
        else if (ContainsMutationSignal(request.Code))
        {
            result = Denied("codeact.mutation_denied", "The fake CodeAct runtime denied code that appears to mutate host or external state.", trace);
        }
        else
        {
            var stdout = request.Code.Contains("hello", StringComparison.OrdinalIgnoreCase)
                ? "hello from CodeAct fake runtime"
                : "CodeAct fake runtime completed read-only execution";

            result = new CodeActResult
            {
                Success = true,
                Content = stdout,
                Stdout = stdout,
                Data = new
                {
                    request.RequestId,
                    request.Language,
                    request.Objective,
                    Runtime = Name
                },
                Trace = trace
            };
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.Trace.Add(new CodeActTrace
        {
            Event = "runtime.completed",
            Component = Name,
            Message = result.Success ? "Fake CodeAct runtime completed successfully." : "Fake CodeAct runtime completed with a denial or error.",
            Attributes = new Dictionary<string, object?>
            {
                ["success"] = result.Success,
                ["error_code"] = result.ErrorCode,
                ["duration_ms"] = result.Duration.TotalMilliseconds
            }
        });

        return Task.FromResult(result);
    }

    private static CodeActResult Denied(string errorCode, string message, List<CodeActTrace> trace)
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

    private static bool IsSupportedLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) ||
               language.Equals("text", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("python", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsMutationSignal(string code)
    {
        var normalized = code.ToLowerInvariant();
        return MutationSignals.Any(normalized.Contains);
    }
}
