using A2A;
using Microsoft.Extensions.Logging;
using System.Net.ServerSentEvents;
using System.Text.Json;

namespace BotSharp.Plugin.A2A.Services;

public class A2AService : IA2AService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<A2AService> _logger;
    private readonly IServiceProvider _services;

    private readonly Dictionary<string, A2AClient> _clientCache = new Dictionary<string, A2AClient>();

    public A2AService(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, ILogger<A2AService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _services = serviceProvider;
        _logger = logger;
    }

    public async Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint, CancellationToken cancellationToken = default)
    {
        var resolver = new A2ACardResolver(new Uri(agentEndpoint));
        return await resolver.GetAgentCardAsync();
    }

    public async Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken)
    {

        if (!_clientCache.TryGetValue(agentEndpoint, out var client))
        {
            HttpClient httpclient = new HttpClient() {  Timeout = TimeSpan.FromSeconds(100) };

            client = new A2AClient(new Uri(agentEndpoint), httpclient);
            _clientCache[agentEndpoint] = client;
        }

        var messagePayload = new AgentMessage
        {
            Role = MessageRole.User, 
            ContextId = contextId,           
            Parts = new List<Part>
            {
                new TextPart { Text = text }
            }
        };

        var sendParams = new MessageSendParams
        {
            Message = messagePayload
        };

        try
        {
            _logger.LogInformation($"Sending A2A message to {agentEndpoint}. ContextId: {contextId}");          
            var responseBase = await client.SendMessageAsync(sendParams, cancellationToken);

            if (responseBase is AgentMessage responseMsg)
            {                
                if (responseMsg.Parts != null && responseMsg.Parts.Any())
                {
                    var textPart = responseMsg.Parts.First() as TextPart;
                    return textPart?.Text ?? string.Empty;
                }
            }
            else if( responseBase is AgentTask atask)
            {
                return $"Task created with ID: {atask.Id}, Status: {atask.Status}";
            }
            else
            {
                return "Unexpected task type.";
            }

            return string.Empty;
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

    public async Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<SseItem<A2AEvent>, Task>? onStreamingEventReceived, CancellationToken cancellationToken = default)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));

        AgentMessage userMessage = new()
        {
            Role = MessageRole.User,
            Parts = parts
        };

        await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = userMessage }))
        {
            await onStreamingEventReceived?.Invoke(sseItem);
        }

        Console.WriteLine(" Streaming completed.");
    }

    public async Task ListenForTaskEventAsync(string endPoint, string taskId, Func<SseItem<A2AEvent>, ValueTask>? onTaskEventReceived = null, CancellationToken cancellationToken = default)
    {

        if (onTaskEventReceived == null)
        {
            return;
        }

        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));

        await foreach (SseItem<A2AEvent> sseItem in client.SubscribeToTaskAsync(taskId))
        {
            await onTaskEventReceived.Invoke(sseItem);
            Console.WriteLine(" Task event received: " + JsonSerializer.Serialize(sseItem.Data));
        }

    }

    public async Task SetPushNotifications(string endPoint, PushNotificationConfig config, CancellationToken cancellationToken = default)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        await client.SetPushNotificationAsync(new TaskPushNotificationConfig()
        {
            PushNotificationConfig = config
        });
    }

    public async Task<AgentTask> CancelTaskAsync(string endPoint, string taskId, CancellationToken cancellationToken = default)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        return await client.CancelTaskAsync(taskId);
    }

    public async Task<AgentTask> GetTaskAsync(string endPoint, string taskId, CancellationToken cancellationToken = default)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        return await client.GetTaskAsync(taskId);
    }

}
