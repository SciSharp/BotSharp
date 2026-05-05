using A2A;
using Microsoft.Agents.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.A2A.Services;

public interface IA2AService
{
    Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken = default);

    Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint, CancellationToken cancellationToken = default);

    Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<AgentResponseUpdate, Task>? onStreamingEventReceived, CancellationToken cancellationToken = default);
 }
