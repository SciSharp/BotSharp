namespace BotSharp.Plugin.AgentTesting.Runtime;

public class MockFunctionExecutor : IFunctionExecutor
{
    public const string BlockedPrefix = "[agent-test] blocked unmocked tool";

    private readonly ActiveTestRun _run;
    private readonly string _functionName;
    private readonly IConversationStateService _state;
    private readonly ILogger _logger;

    public MockFunctionExecutor(
        ActiveTestRun run,
        string functionName,
        IConversationStateService state,
        ILogger logger)
    {
        _run = run;
        _functionName = functionName;
        _state = state;
        _logger = logger;
    }

    public Task<string> GetIndicatorAsync(RoleDialogModel message) => Task.FromResult(string.Empty);

    public Task<bool> ExecuteAsync(RoleDialogModel message)
    {
        if (_functionName == AgentTestCanary.FunctionName)
        {
            _run.CanaryIntercepted = true;
            message.Content = AgentTestCanary.ExpectedContent;
            return Task.FromResult(true);
        }

        var ordinal = _run.NextCallOrdinal(_functionName);
        var mock = ToolMockMatcher.Match(_run.Mocks, _functionName, message.FunctionArgs, ordinal);

        if (mock == null)
        {
            message.Content = $"{BlockedPrefix}: {_functionName}";
            message.StopCompletion = true;
            _run.Record(new ObservedToolCall
            {
                TurnIndex = _run.CurrentTurnIndex,
                FunctionName = _functionName,
                ArgsJson = message.FunctionArgs,
                Outcome = "Blocked",
                ResultContent = message.Content
            });
            return Task.FromResult(false);
        }

        message.Content = mock.ResultContent;
        if (mock.StopCompletion)
        {
            message.StopCompletion = true;
        }

        foreach (var write in mock.StateWrites ?? [])
        {
            _state.SetState(write.Key, write.Value, activeRounds: write.ActiveRounds);
        }

        _run.Record(new ObservedToolCall
        {
            TurnIndex = _run.CurrentTurnIndex,
            FunctionName = _functionName,
            ArgsJson = message.FunctionArgs,
            Outcome = "Mocked",
            ResultContent = mock.ResultContent
        });

        return Task.FromResult(true);
    }
}
