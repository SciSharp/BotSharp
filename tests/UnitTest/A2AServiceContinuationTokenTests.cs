using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Core.A2A.Services;
using BotSharp.Core.A2A.Settings;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UnitTest;

[TestClass]
public class A2AServiceContinuationTokenTests
{
    private const string Endpoint = "https://remote-agent.example.com";
    private const string ConversationId = "conv-001";

    [TestMethod]
    public void ContinuationToken_ShouldPersistToConversationState_AfterUpdate()
    {
#pragma warning disable MEAI001
        var state = new InMemoryConversationStateService();
        var service = CreateService(state);
    var continuationToken = ResponseContinuationToken.FromBytes(new byte[] { 1, 2, 3 });

        InvokePrivate(
            service,
            "UpdateContinuationToken",
            Endpoint,
            ConversationId,
            continuationToken);

        var stateKey = BuildExpectedStateKey(Endpoint, ConversationId);
        var persisted = state.GetState(stateKey);

        Assert.IsFalse(string.IsNullOrWhiteSpace(persisted));
        Assert.IsTrue(state.ContainsState(stateKey));
#pragma warning restore MEAI001
    }

    [TestMethod]
    public void ContinuationToken_ShouldRestoreFromState_InNewServiceScope()
    {
#pragma warning disable MEAI001
        var sharedState = new InMemoryConversationStateService();
        var serviceFromScope1 = CreateService(sharedState);
    var continuationToken = ResponseContinuationToken.FromBytes(new byte[] { 1, 2, 3 });

        InvokePrivate(
            serviceFromScope1,
            "UpdateContinuationToken",
            Endpoint,
            ConversationId,
            continuationToken);

        var serviceFromScope2 = CreateService(sharedState);
        var options = InvokePrivate<AgentRunOptions?>(
            serviceFromScope2,
            "GetRunOptions",
            Endpoint,
            ConversationId);

        Assert.IsNotNull(options);
        Assert.IsNotNull(options.ContinuationToken);
#pragma warning restore MEAI001
    }

    [TestMethod]
    public void CorruptedTokenState_ShouldBeIgnored_AndRemoved()
    {
#pragma warning disable MEAI001
        var state = new InMemoryConversationStateService();
        var logger = new TestLogger<A2AService>();
        var service = CreateService(state, logger);
        var stateKey = BuildExpectedStateKey(Endpoint, ConversationId);

        state.SetState(
            stateKey,
            "this-is-not-json",
            isNeedVersion: false,
            source: StateSource.Application);

        var options = InvokePrivate<AgentRunOptions?>(
            service,
            "GetRunOptions",
            Endpoint,
            ConversationId);

        Assert.IsNull(options);
        Assert.IsFalse(state.ContainsState(stateKey));
        Assert.IsTrue(logger.Logs.Any(x => x.Level == LogLevel.Warning));
#pragma warning restore MEAI001
    }

    private static A2AService CreateService(
        IConversationStateService state,
        ILogger<A2AService>? logger = null)
    {
        return new A2AService(
            httpClientFactory: new DummyHttpClientFactory(),
            services: new DummyServiceProvider(),
            logger: logger ?? new TestLogger<A2AService>(),
            settings: new A2ASettings(),
            conversationState: state);
    }

    private static string BuildExpectedStateKey(string agentEndpoint, string conversationId)
    {
        var endpointHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(agentEndpoint)));
        return $"a2a:continuation-token:{conversationId}:{endpointHash}";
    }

    private static void InvokePrivate(object instance, string methodName, params object?[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Private method '{methodName}' was not found.");
        method!.Invoke(instance, args);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object?[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Private method '{methodName}' was not found.");
        return (T)method!.Invoke(instance, args)!;
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class InMemoryConversationStateService : IConversationStateService
    {
        private readonly Dictionary<string, string> _states = new();

        public string GetConversationId() => ConversationId;

        public Task<Dictionary<string, string>> Load(string conversationId, bool isReadOnly = false)
            => Task.FromResult(new Dictionary<string, string>(_states));

        public string GetState(string name, string defaultValue = "")
            => _states.TryGetValue(name, out var value) ? value : defaultValue;

        public bool ContainsState(string name) => _states.ContainsKey(name);

        public Dictionary<string, string> GetStates() => new(_states);

        public IConversationStateService SetState<T>(
            string name,
            T value,
            bool isNeedVersion = true,
            int activeRounds = -1,
            string valueType = StateDataType.String,
            string source = StateSource.User,
            bool readOnly = false)
        {
            if (value != null)
            {
                _states[name] = value.ToString() ?? string.Empty;
            }

            return this;
        }

        public void SaveStateByArgs(JsonDocument args)
        {
        }

        public bool RemoveState(string name) => _states.Remove(name);

        public void CleanStates(params string[] excludedStates)
        {
            var keep = new HashSet<string>(excludedStates ?? Array.Empty<string>());
            var keysToDelete = _states.Keys.Where(k => !keep.Contains(k)).ToList();
            foreach (var key in keysToDelete)
            {
                _states.Remove(key);
            }
        }

        public Task Save() => Task.CompletedTask;

        public ConversationState GetCurrentState() => new();

        public void SetCurrentState(ConversationState state)
        {
        }

        public void ResetCurrentState()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Logs { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Logs.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
