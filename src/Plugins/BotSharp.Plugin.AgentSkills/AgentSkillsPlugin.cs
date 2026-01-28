using AgentSkillsDotNet;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.AgentSkills.Functions;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.AI;
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
        // 修复：使用 Get<T> 获取配置对象
        AgentSkillsSettings agentskillSettings = config.GetSection("AgentSkills").Get<AgentSkillsSettings>();
        // Register settings
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return agentskillSettings = settingService.Bind<AgentSkillsSettings>("AgentSkills");
        }); 
        var skillFactory = new AgentSkillsFactory();
        var agentSkills = skillFactory.GetAgentSkills(agentskillSettings.ProjectSkillsDir);
        services.AddSingleton(skillFactory);

        IList<AITool> tools = agentSkills.GetAsTools(AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools, new AgentSkillsAsToolsOptions
        {
            IncludeToolForFileContentRead = false
        });

        foreach (var tool in tools) {
            services.AddSingleton<AIFunction>(tool as AIFunction);
            services.AddScoped<IFunctionCallback>(sp =>
                new AIToolCallbackAdapter(tool as AIFunction , sp));
        }

        // 注册 BotSharp Hook
        services.AddScoped<IAgentHook, AgentSkillsIntegrationHook>();
        services.AddScoped<IConversationHook,AgentSkillsConversationHook>();
    }
}
