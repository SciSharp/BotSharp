using A2A;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Core.A2A.Settings;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BotSharp.Core.A2A.Services;


public class A2AService : IA2AService
{
    private const string ContinuationTokenStatePrefix = "a2a:continuation-token";

    // Protocol binding name constants from the A2A v1 specification.
    private const string BindingHttpJson = "http+json";
    private const string BindingJsonRpc = "json-rpc";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<A2AService> _logger;
    private readonly IConversationStateService _conversationState;
    private readonly IServiceProvider _services;
    private readonly A2ASettings _settings;

    // High-level A2A v1 agent cache
    private readonly Dictionary<string, AIAgent> _aiAgentCache = new();
#pragma warning disable MEAI001
    private readonly Dictionary<string, ResponseContinuationToken> _continuationTokenCache = new();
#pragma warning restore MEAI001

    // LEGACY: Used for task APIs and the StreamResponse compatibility overload.
    private readonly Dictionary<string, A2AClient> _clientCache = new();

    public A2AService(
        IHttpClientFactory httpClientFactory,
        IServiceProvider services,
        ILogger<A2AService> logger,
        A2ASettings settings,
        IConversationStateService conversationState)
    {
        _httpClientFactory = httpClientFactory;
        _services = services;
        _logger = logger;
        _settings = settings;
        _conversationState = conversationState;
    } 

    public async Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint, CancellationToken cancellationToken = default)
    {
        var endpointUri = CreateValidatedEndpointUri(agentEndpoint, nameof(agentEndpoint));
        var resolver = new A2ACardResolver(endpointUri);
        return await resolver.GetAgentCardAsync(cancellationToken);
    }

    private async Task<AIAgent> CreateAIAgentAsync(string agentEndpoint, CancellationToken cancellationToken = default)
    {
        var endpointUri = CreateValidatedEndpointUri(agentEndpoint, nameof(agentEndpoint));

        if (_aiAgentCache.TryGetValue(agentEndpoint, out var cachedAgent))
        {
            return cachedAgent;
        }

        var resolver = new A2ACardResolver(endpointUri);
        var aiAgent = await resolver.GetAIAgentAsync();
        _aiAgentCache[agentEndpoint] = aiAgent;
        return aiAgent;
    }

    private Uri CreateValidatedEndpointUri(string endpoint, string paramName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogWarning("A2A endpoint parameter {ParamName} is null or empty.", paramName);
            throw new ArgumentException("A2A endpoint is required and cannot be null or empty.", paramName);
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            (endpointUri.Scheme != Uri.UriSchemeHttp && endpointUri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("A2A endpoint parameter {ParamName} is invalid: {Endpoint}", paramName, endpoint);
            throw new ArgumentException("A2A endpoint must be a valid absolute HTTP or HTTPS URI.", paramName);
        }

        return endpointUri;
    }

    private static string BuildSessionCacheKey(string agentEndpoint, string contextId)
        => $"{agentEndpoint}::{contextId}";

    private static string BuildContinuationTokenStateKey(string agentEndpoint, string contextId)
    {
        var endpointHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(agentEndpoint)));
        return $"{ContinuationTokenStatePrefix}:{contextId}:{endpointHash}";
    }

#pragma warning disable MEAI001
    private AgentRunOptions? GetRunOptions(string agentEndpoint, string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
        {
            return null;
        }

        var cacheKey = BuildSessionCacheKey(agentEndpoint, contextId);
        if (!_continuationTokenCache.TryGetValue(cacheKey, out var continuationToken))
        {
            continuationToken = ReadContinuationTokenFromConversationState(agentEndpoint, contextId);
            if (continuationToken == null)
            {
                return null;
            }

            _continuationTokenCache[cacheKey] = continuationToken;
        }

        return new AgentRunOptions
        {
            ContinuationToken = continuationToken
        };
    }

    private void UpdateContinuationToken(string agentEndpoint, string contextId, ResponseContinuationToken? continuationToken)
    {
        if (string.IsNullOrWhiteSpace(contextId) || continuationToken == null)
        {
            return;
        }

        var cacheKey = BuildSessionCacheKey(agentEndpoint, contextId);
        _continuationTokenCache[cacheKey] = continuationToken;
        PersistContinuationTokenToConversationState(agentEndpoint, contextId, continuationToken);
    }

    private ResponseContinuationToken? ReadContinuationTokenFromConversationState(string agentEndpoint, string contextId)
    {
        var stateKey = BuildContinuationTokenStateKey(agentEndpoint, contextId);
        var serializedToken = _conversationState.GetState(stateKey);
        if (string.IsNullOrWhiteSpace(serializedToken))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ResponseContinuationToken>(serializedToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to deserialize A2A continuation token for context {ContextId} and endpoint {AgentEndpoint}.",
                contextId,
                agentEndpoint);
            _conversationState.RemoveState(stateKey);
            return null;
        }
    }

    private void PersistContinuationTokenToConversationState(string agentEndpoint, string contextId, ResponseContinuationToken continuationToken)
    {
        var stateKey = BuildContinuationTokenStateKey(agentEndpoint, contextId);

        try
        {
            var serializedToken = JsonSerializer.Serialize(continuationToken);
            _conversationState.SetState(
                stateKey,
                serializedToken,
                isNeedVersion: false,
                source: StateSource.Application);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to persist A2A continuation token for context {ContextId} and endpoint {AgentEndpoint}.",
                contextId,
                agentEndpoint);
        }
    }
#pragma warning restore MEAI001

    // HIGH-LEVEL: Preferred A2A v1 API for message sending
    public async Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken)
    {
        try
        {
            var agent = await CreateAIAgentAsync(agentEndpoint, cancellationToken);
            var runOptions = GetRunOptions(agentEndpoint, contextId);
            _logger.LogInformation("Sending A2A message via AIAgent to {AgentEndpoint}. ContextId: {ContextId}", agentEndpoint, contextId);
            var response = await agent.RunAsync(
                message: text ?? string.Empty,
                options: runOptions,
                cancellationToken: cancellationToken);

#pragma warning disable MEAI001
            UpdateContinuationToken(agentEndpoint, contextId, response.ContinuationToken);
#pragma warning restore MEAI001
            return response.Text ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Network error communicating with A2A agent at {agentEndpoint}");
            throw new Exception($"Remote agent unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"A2A Protocol error: {ex.Message}");
            throw;
        }
    }

    // HIGH-LEVEL: Streaming uses AIAgent.RunStreamingAsync in A2A v1.
    public async Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<AgentResponseUpdate, Task>? onStreamingEventReceived, CancellationToken cancellationToken = default)
    {
        var safeParts = parts?.ToList() ?? new List<Part>();

        var userMessage = new Message
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Role = Role.User,
            Parts = safeParts
        };

        var agent = await CreateAIAgentAsync(endPoint, cancellationToken);
        var chatMessage = userMessage.ToChatMessage();

        await foreach (var streamResponse in agent.RunStreamingAsync(
            messages: new[] { chatMessage }, 
            options: null,
            cancellationToken: cancellationToken))
        {
            if (onStreamingEventReceived != null)
                await onStreamingEventReceived(streamResponse);
        }

        _logger.LogInformation("Streaming completed.");
    }  
}
