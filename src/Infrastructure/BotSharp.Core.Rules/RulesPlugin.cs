using BotSharp.Core.Rules.Engines;

namespace BotSharp.Core.Rules;

public class RulesPlugin : IBotSharpPlugin
{
    public string Id => "0197c1bc-9ae6-4c56-a305-8a1b4095bebc";
    public string Name => "BotSharp Rules";
    public string Description => "Translates user-defined natural language rules into programmatic code and is responsible for executing these rules under user-specified conditions.";
    public string IconUrl => "https://w7.pngwing.com/pngs/442/614/png-transparent-regulation-computer-icons-regulatory-compliance-medical-device-manufacturing-others-miscellaneous-blue-text-thumbnail.png";

    public string[] AgentIds =
    [
        BuiltInAgentId.RulesInterpreter
    ];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IRuleEngine, RuleEngine>();
    }
}
