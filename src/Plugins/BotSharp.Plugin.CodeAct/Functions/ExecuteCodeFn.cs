namespace BotSharp.Plugin.CodeAct.Functions;

public class ExecuteCodeFn : IFunctionCallback
{
    private readonly ICodeActRuntime _runtime;
    private readonly CodeActSettings _settings;

    public string Name => "execute_code";
    public string Indication => "Executing code in a restricted read-only runtime.";

    public ExecuteCodeFn(ICodeActRuntime runtime, CodeActSettings settings)
    {
        _runtime = runtime;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        ExecuteCodeArgs? args;
        try
        {
            args = JsonSerializer.Deserialize<ExecuteCodeArgs>(message.FunctionArgs ?? "{}");
        }
        catch (JsonException ex)
        {
            return SetFailure(message, "codeact.invalid_arguments", $"Invalid execute_code arguments: {ex.Message}");
        }

        if (args == null || string.IsNullOrWhiteSpace(args.Code))
        {
            return SetFailure(message, "codeact.empty_code", "execute_code requires a non-empty code argument.");
        }

        var request = new CodeActRequest
        {
            MessageId = message.MessageId,
            AgentId = message.CurrentAgentId,
            UserId = message.SenderId,
            Language = string.IsNullOrWhiteSpace(args.Language) ? "python" : args.Language,
            Code = args.Code,
            Objective = args.Objective,
            ReadOnly = _settings.ReadOnlyPilot || args.ReadOnly,
            Metadata = args.Metadata
        };

        var result = await _runtime.ExecuteAsync(request);
        message.Content = result.Content;
        message.Data = result;
        message.StopCompletion = true;
        return result.Success;
    }

    private static bool SetFailure(RoleDialogModel message, string errorCode, string errorMessage)
    {
        var result = new CodeActResult
        {
            Success = false,
            Content = errorMessage,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Trace =
            [
                new CodeActTrace
                {
                    Event = "execute_code.validation_failed",
                    Component = nameof(ExecuteCodeFn),
                    Message = errorMessage,
                    Attributes = new Dictionary<string, object?> { ["error_code"] = errorCode }
                }
            ]
        };

        message.Content = errorMessage;
        message.Data = result;
        message.StopCompletion = true;
        return false;
    }
}
