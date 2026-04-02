using A2A;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BotSharp.Core.A2A.Services;

public class A2AService : IA2AService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<A2AService> _logger;
    private readonly IServiceProvider _services;

    private readonly Dictionary<string, A2AClient> _clientCache = new Dictionary<string, A2AClient>();

    public A2AService(IHttpClientFactory httpClientFactory, IServiceProvider services, ILogger<A2AService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _services = services;
        _logger = logger;
    }

    private async Task<A2AClient> CreateClientAsync(string agentEndpoint, CancellationToken cancellationToken = default)
    {
        if (_clientCache.TryGetValue(agentEndpoint, out var cachedClient))
        {
            return cachedClient;
        }

        var agentCard = await GetCapabilitiesAsync(agentEndpoint, cancellationToken);
        var clientEndpoint = agentCard.SupportedInterfaces.FirstOrDefault()?.Url ?? agentEndpoint;
        var client = new A2AClient(new Uri(clientEndpoint), _httpClientFactory.CreateClient());
        _clientCache[agentEndpoint] = client;
        return client;
    }

    public async Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint, CancellationToken cancellationToken = default)
    {
        var resolver = new A2ACardResolver(new Uri(agentEndpoint));
        return await resolver.GetAgentCardAsync();
    }

    public async Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(agentEndpoint, cancellationToken);

        var messagePayload = new Message
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Role = Role.User,
            ContextId = contextId,           
            Parts = [Part.FromText(text)]
        };

        var sendRequest = new SendMessageRequest
        {    
            Message = messagePayload
        };

        try
        {
            _logger.LogInformation($"Sending A2A message to {agentEndpoint}. ContextId: {contextId}");          
            var response = await client.SendMessageAsync(sendRequest, cancellationToken);

            return response.PayloadCase switch
            {
                SendMessageResponseCase.Message => response.Message?.Parts?.FirstOrDefault()?.Text ?? string.Empty,
                SendMessageResponseCase.Task => $"Task created with ID: {response.Task?.Id}, Status: {response.Task?.Status}",
                _ => "Unexpected task type."
            };
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

    public async Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<StreamResponse, Task>? onStreamingEventReceived, CancellationToken cancellationToken = default)
    {
        A2AClient client = await CreateClientAsync(endPoint, cancellationToken);

        Message userMessage = new()
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Role = Role.User,
            Parts = parts
        };

        await foreach (StreamResponse streamResponse in client.SendStreamingMessageAsync(new SendMessageRequest { Message = userMessage }, cancellationToken))
        {
            await onStreamingEventReceived?.Invoke(streamResponse);
        }

        Console.WriteLine(" Streaming completed.");
    }

    public async Task ListenForTaskEventAsync(string endPoint, string taskId, Func<StreamResponse, ValueTask>? onTaskEventReceived = null, CancellationToken cancellationToken = default)
    {

        if (onTaskEventReceived == null)
        {
            return;
        }

        A2AClient client = await CreateClientAsync(endPoint, cancellationToken);

        await foreach (StreamResponse streamResponse in client.SubscribeToTaskAsync(new SubscribeToTaskRequest { Id = taskId }, cancellationToken))
        {
            await onTaskEventReceived.Invoke(streamResponse);
            Console.WriteLine(" Task event received: " + JsonSerializer.Serialize(streamResponse));
        }

    }

    public async Task SetPushNotifications(string endPoint, string taskId, PushNotificationConfig config, CancellationToken cancellationToken = default)
    {
        A2AClient client = await CreateClientAsync(endPoint, cancellationToken);
        await client.CreateTaskPushNotificationConfigAsync(new CreateTaskPushNotificationConfigRequest()
        {
            TaskId = taskId,
            Config = config
        }, cancellationToken);
    }

    public async Task<AgentTask> CancelTaskAsync(string endPoint, string taskId, CancellationToken cancellationToken = default)
    {
        A2AClient client = await CreateClientAsync(endPoint, cancellationToken);
        return await client.CancelTaskAsync(new CancelTaskRequest { Id = taskId }, cancellationToken);
    }

    public async Task<AgentTask> GetTaskAsync(string endPoint, string taskId, CancellationToken cancellationToken = default)
    {
        A2AClient client = await CreateClientAsync(endPoint, cancellationToken);
        return await client.GetTaskAsync(new GetTaskRequest { Id = taskId }, cancellationToken);
    }

}
