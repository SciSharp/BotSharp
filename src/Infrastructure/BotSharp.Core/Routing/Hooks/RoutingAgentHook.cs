using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Enums;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Hooks;

public class RoutingAgentHook : AgentHookBase
{
    private readonly RoutingSettings _routingSetting;
    public override string SelfId => string.Empty;

    public RoutingAgentHook(IServiceProvider services, AgentSettings settings, RoutingSettings routingSetting) 
        : base(services, settings)
    {
        _routingSetting = routingSetting;
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        if (_agent.Type != AgentType.Routing)
        {
            return base.OnInstructionLoaded(template, dict);
        }
        dict["router"] = _agent;

        var routing = _services.GetRequiredService<IRoutingService>();
        var agents = routing.GetRoutableAgents(_agent.Profiles);

        // Postprocess agent required fields, remove it if the states exists
        var states = _services.GetRequiredService<IConversationStateService>();
        foreach (var agent in agents)
        {
            var fields = agent.RequiredFields.ToArray();
            foreach (var field in fields)
            {
                if (states.ContainsState(field.Name))
                {
                    var requiredField = agent.RequiredFields.First(x => x.Name == field.Name);
                    agent.RequiredFields.Remove(requiredField);
                }
            }

            fields = agent.OptionalFields.ToArray();
            foreach (var field in fields)
            {
                if (states.ContainsState(field.Name))
                {
                    var optionalField = agent.OptionalFields.First(x => x.Name == field.Name);
                    agent.OptionalFields.Remove(optionalField);
                }
            }
        }

        dict["routing_agents"] = agents;
        dict["routing_handlers"] = routing.GetHandlers(_agent);

        return base.OnInstructionLoaded(template, dict);
    }

    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        if (_agent.Type == AgentType.Task)
        {
            // check if enabled the routing rule
            var routing = _services.GetRequiredService<IRoutingService>();
            var rule = routing.GetRulesByAgentId(_agent.Id)
                .FirstOrDefault(x => x.Type == RuleType.Fallback);
            if (rule != null)
            {
                var agentService = _services.GetRequiredService<IAgentService>();
                var redirectAgent = agentService.GetAgent(rule.RedirectTo).Result;

                var json = JsonSerializer.Serialize(new
                {
                    user_goal_agent = new
                    {
                        type = "string",
                        description = $"{_agent.Name}"
                    },
                    next_action_agent = new
                    {
                        type = "string",
                        description = $"{redirectAgent.Name}"
                    },
                    reason = new
                    {
                        type = "string",
                        description = $"the reason why you need to fallback to [{redirectAgent.Name}] from [{_agent.Name}]"
                    },
                });
                functions.Add(new FunctionDef
                {
                    Name = "fallback_to_router",
                    Description = $"If the user's request is beyond your capabilities, you can call this function to handle by other agent ({redirectAgent.Name}).",
                    Parameters =
                    {
                        Properties = JsonSerializer.Deserialize<JsonDocument>(json),
                        Required = new List<string>
                        {
                            "user_goal_agent",
                            "next_action_agent",
                            "reason"
                        }
                    }
                });
            }

            var settings = _services.GetRequiredService<AgentSettings>();
            if (settings.EnableHttpHandler)
            {
                var httpHandlerName = "handle_http_request";
                var existHttpHandler = functions.Any(x => x.Name == httpHandlerName);
                var funcs = _services.GetServices<IFunctionCallback>();
                var httpRequestFunc = funcs.FirstOrDefault(x => x.Name == httpHandlerName);
                if (!existHttpHandler && httpRequestFunc != null)
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        request_url = new
                        {
                            type = "string",
                            description = $"The http url that is requested. It can be an absolute url that starts with \"http\" or \"https\", or a relative url that starts with \"/\""
                        },
                        http_method = new
                        {
                            type = "string",
                            description = $"The http method that is requested, e.g., GET, POST, PUT, and DELETE."
                        },
                        request_content = new
                        {
                            type = "string",
                            description = $"The http request content. It must be in json format."
                        }
                    });
                    functions.Add(new FunctionDef
                    {
                        Name = httpRequestFunc.Name,
                        Description = "If the user requests to send an http request, you need to capture the http method and request content, and then call this function to send the http request.",
                        Parameters =
                        {
                            Properties = JsonSerializer.Deserialize<JsonDocument>(json),
                            Required = new List<string>
                            {
                                "request_url",
                                "http_method"
                            }
                        }
                    });
                }

            }
        }

        return base.OnFunctionsLoaded(functions);
    }
}
