using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    /// <summary>
    /// Sometimes LLM hallucinates and fails to set function names correctly.
    /// </summary>
    /// <param name="args"></param>
    private void FixMalformedResponse(FunctionCallFromLlm args)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = agentService.GetAgents(allowRouting: true).Result;
        var malformed = false;

        // Sometimes it populate malformed Function in Agent name
        if (!string.IsNullOrEmpty(args.Function) && 
            args.Function == args.AgentName)
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Another case of malformed response
        if (string.IsNullOrEmpty(args.AgentName) && 
            agents.Select(x => x.Name).Contains(args.Function))
        {
            args.AgentName = args.Function;
            args.Function = "route_to_agent";
            malformed = true;
        }

        // It should be Route to agent, but it is used as Response to user.
        if (!string.IsNullOrEmpty(args.AgentName) &&
            agents.Select(x => x.Name).Contains(args.AgentName) &&
            args.Function != "route_to_agent")
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Function name shouldn't contain dot symbol
        if (!string.IsNullOrEmpty(args.Function) &&
            args.Function.Contains('.'))
        {
            args.Function = args.Function.Split('.').Last();
            malformed = true;
        }

        if (malformed)
        {
            _logger.LogWarning($"Captured LLM malformed response");
        }
    }
}
