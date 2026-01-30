using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills.Tests;

/// <summary>
/// Simple test logger implementation for unit tests.
/// </summary>
/// <typeparam name="T">The type being logged.</typeparam>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<string> _logMessages = new();

    public IReadOnlyList<string> LogMessages => _logMessages.AsReadOnly();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _logMessages.Add($"[{logLevel}] {message}");
    }
}
