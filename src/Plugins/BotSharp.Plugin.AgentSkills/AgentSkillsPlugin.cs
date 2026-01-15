using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.AgentSkills.Functions;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.AgentSkills;

/// <summary>
/// Agent Skills plugin for BotSharp.
/// Enables AI agents to leverage reusable skills following the Agent Skills specification.
/// </summary>
public class AgentSkillsPlugin : IBotSharpPlugin
{
    public string Id => "a5b3e8c1-7d2f-4a9e-b6c4-8f5d1e2a3b4c";
    public string Name => "Agent Skills";
    public string Description => "Enables AI agents to leverage reusable skills following the Agent Skills specification (https://agentskills.io).";
    public string IconUrl => "https://raw.githubusercontent.com/SciSharp/BotSharp/master/docs/static/logos/BotSharp.png";
    public string[] AgentIds => [];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register settings
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AgentSkillsSettings>("AgentSkills");
        });

        // Register skill loader
        services.AddScoped<SkillLoader>();

        // Register hooks
        services.AddScoped<IAgentUtilityHook, AgentSkillsUtilityHook>();

        // Register function callbacks
        services.AddScoped<IFunctionCallback, ReadSkillFn>();
        services.AddScoped<IFunctionCallback, ReadSkillFileFn>();
        services.AddScoped<IFunctionCallback, ListSkillDirectoryFn>();
    }
}
