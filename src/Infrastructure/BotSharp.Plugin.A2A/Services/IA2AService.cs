using A2A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.A2A.Services;

public interface IA2AService
{
    Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken);
    Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint);
}
