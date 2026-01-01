using A2A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.A2A.Services;

public interface IA2AService
{
    Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken = default);

    Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint, CancellationToken cancellationToken = default);

    Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<SseItem<A2AEvent>, Task>? onStreamingEventReceived,CancellationToken cancellationToken = default);

    Task ListenForTaskEventAsync(string endPoint, string taskId, Func<SseItem<A2AEvent>, ValueTask>? onTaskEventReceived = null, CancellationToken cancellationToken = default);

    Task SetPushNotifications(string endPoint, PushNotificationConfig config, CancellationToken cancellationToken = default);

    Task<AgentTask> CancelTaskAsync(string endPoint, string taskId, CancellationToken cancellationToken = default);

    Task<AgentTask> GetTaskAsync(string endPoint, string taskId, CancellationToken cancellationToken);
}
