namespace BotSharp.Core.Routing.Reasoning;

public static class ReasonerHelper
{
    /// <summary>
    /// Sometimes LLM hallucinates and fails to set function names correctly.
    /// </summary>
    /// <param name="args"></param>
    public static void FixMalformedResponse(IServiceProvider services, FunctionCallFromLlm args)
    {
        var agentService = services.GetRequiredService<IAgentService>();
        var agents = agentService.GetAgents(new AgentFilter
        {
            Type = AgentType.Task
        }).Result.Items.ToList();
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

        // Agent Name is contaminated.
        if (args.Function == "route_to_agent")
        {
            // Action agent name
            if (!agents.Any(x => x.Name == args.AgentName) && !string.IsNullOrEmpty(args.AgentName))
            {
                args.AgentName = agents.FirstOrDefault(x => args.AgentName.Contains(x.Name))?.Name ?? args.AgentName;
            }

            // Goal agent name
            if (!agents.Any(x => x.Name == args.OriginalAgent) && !string.IsNullOrEmpty(args.OriginalAgent))
            {
                args.OriginalAgent = agents.FirstOrDefault(x => args.OriginalAgent.Contains(x.Name))?.Name ?? args.OriginalAgent;
            }
        }

        if (malformed)
        {
            Console.WriteLine($"Captured LLM malformed response");
        }
    }
}
