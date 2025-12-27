using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Plugin.A2A.Services;
using BotSharp.Plugin.A2A.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.A2A.Hooks;

public class A2AAgentHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    private readonly A2ASettings _settings;
    private readonly IA2AService _iA2AService;

    public A2AAgentHook(IServiceProvider services, IA2AService a2AService, A2ASettings settings)
        : base(services, new AgentSettings())
    {
        _iA2AService = a2AService;
        _settings = settings;
    }

    public override bool OnAgentLoading(ref string id)
    {
        var agentId = id;
        var remoteConfig = _settings.Agents.FirstOrDefault(x => x.Id == agentId);
        if (remoteConfig != null)
        {
            return true;
        }
        return base.OnAgentLoading(ref id);
    }

    public override void OnAgentLoaded(Agent agent)
    {
        // Check if this is an A2A remote agent
        if (agent.Type != AgentType.A2ARemote)
        {
            return;
        }

        var remoteConfig = _settings.Agents.FirstOrDefault(x => x.Id == agent.Id);
        if (remoteConfig != null)
        {
            var agentCard = _iA2AService.GetCapabilitiesAsync(remoteConfig.Endpoint).GetAwaiter().GetResult();
            agent.Name = agentCard.Name;
            agent.Description = agentCard.Description;
            agent.Instruction = $"You are a proxy interface for an external intelligent service named '{agentCard.Name}'. " +
                                $"Your ONLY goal is to forward the user's request verbatim to the external service. " +
                                $"You must use the function 'delegate_to_a2a' to communicate with it. " +
                                $"Do not attempt to answer the question yourself.";

            var properties = new Dictionary<string, object>
            {
                {
                    "user_query",
                    new
                    {
                        type = "string",
                        description = "The exact user request or task description to be forwarded."
                    }
                }
            };

            var propertiesJson = JsonSerializer.Serialize(properties);
            var propertiesDocument = JsonDocument.Parse(propertiesJson);

            agent.Functions.Add(new FunctionDef
            {
                Name = "delegate_to_a2a",
                Description = $"Delegates the task to the external {remoteConfig.Name} via A2A protocol.",
                Parameters = new FunctionParametersDef()
                {
                    Type = "object",
                    Properties = propertiesDocument,
                    Required = new List<string> { "user_query" }
                }
            });
        }
        base.OnAgentLoaded(agent);
    }
}
