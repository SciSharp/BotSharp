using A2A;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public async Task<AgentCard> GetCapabilitiesAsync(string agentEndpoint)
    {
        var resolver = new A2ACardResolver(new Uri(agentEndpoint));
        return await resolver.GetAgentCardAsync();
    }

    public async Task<string> SendMessageAsync(string agentEndpoint, string text, string contextId, CancellationToken cancellationToken)
    {

        if (!_clientCache.TryGetValue(agentEndpoint, out var client))
        {     
            client = new A2AClient(new Uri(agentEndpoint));
            _clientCache[agentEndpoint] = client;
        }

        //var agentService = _services.GetRequiredService<IAgentService>();
        //var conv = _services.GetRequiredService<IConversationService>();
        //var routingCtx = _services.GetRequiredService<IRoutingContext>();

        //var wholeDialogs = routingCtx.GetDialogs();
        //if (wholeDialogs.IsNullOrEmpty())
        //{
        //    wholeDialogs = conv.GetDialogHistory();
        //}

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
}
