using BotSharp.Core.Rules.Triggers;

namespace BotSharp.Core.Rules.Engines;

public class RuleEngine : IRuleEngine
{
    private readonly IServiceProvider _services;
    public RuleEngine(IServiceProvider services)
    {
        _services = services;
    }

    public async Task Triggered(IRuleTrigger trigger, string data)
    {
        // Pull all user defined rules
        
        var instructService = _services.GetRequiredService<IInstructService>();

        var userSay = $"===Input data===\r\n{data}\r\n\r\nWhen WO NTE is greater than 100, notify resident.";

        var result = await instructService.Execute(BuiltInAgentId.RulesInterpreter, new RoleDialogModel(AgentRole.User, data),  "criteria_check", "#TEMPLATE#");

        string[] rules = [];
        // Check if meet the criteria
    }
}
