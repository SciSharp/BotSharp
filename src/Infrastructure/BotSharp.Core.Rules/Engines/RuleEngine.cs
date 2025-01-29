using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BotSharp.Core.Rules.Engines;

public class RuleEngine : IRuleEngine
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public RuleEngine(IServiceProvider services, ILogger<RuleEngine> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task Triggered(IRuleTrigger trigger, string data, List<MessageState>? states = null)
    {
        // Pull all user defined rules
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = await agentService.GetAgents(new AgentFilter
        {
            Pager = new Pagination
            {
                Size = 1000
            }
        });

        var preFilteredAgents = agents.Items.Where(x => 
            x.Rules.Exists(r => r.TriggerName == trigger.Name &&
                !x.Disabled)).ToList();

        // Trigger the agents
        var instructService = _services.GetRequiredService<IInstructService>();
        

        foreach (var agent in preFilteredAgents)
        {
            var convService = _services.GetRequiredService<IConversationService>();
            var conv = await convService.NewConversation(new Conversation
            {
                Channel = trigger.Channel,
                Title = data,
                AgentId = agent.Id
            });

            var message = new RoleDialogModel(AgentRole.User, data);

            var allStates = new List<MessageState>
            {
                new("channel", trigger.Channel)
            };

            if (states != null)
            {
                allStates.AddRange(states);
            }

            convService.SetConversationId(conv.Id, allStates);

            await convService.SendMessage(agent.Id,
                message,
                null,
                msg => Task.CompletedTask);

            convService.SaveStates();

            /*foreach (var rule in agent.Rules)
            {
                var userSay = $"===Input data with Before and After values===\r\n{data}\r\n\r\n===Trigger Criteria===\r\n{rule.Criteria}\r\n\r\nJust output 1 or 0 without explanation: ";

                var result = await instructService.Execute(BuiltInAgentId.RulesInterpreter, new RoleDialogModel(AgentRole.User, userSay), "criteria_check", "#TEMPLATE#");

                // Check if meet the criteria
                if (result.Text == "1")
                {
                    // Hit rule
                    _logger.LogInformation($"Hit rule {rule.TriggerName} {rule.EntityType} {rule.EventName}, {data}");

                    await convService.SendMessage(agent.Id, 
                        new RoleDialogModel(AgentRole.User, $"The conversation was triggered by {rule.Criteria}"), 
                        null, 
                        msg => Task.CompletedTask);
                }
            }*/
        }
    }
}
