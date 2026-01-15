using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Settings;
using BotSharp.Plugin.AgentSkills.Hooks;
using BotSharp.Plugin.AgentSkills.Functions;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Functions;

namespace BotSharp.Plugin.AgentSkills;

public class AgentSkillsPlugin : IBotSharpPlugin
{
    public string Id => "b6c93605-246e-4f7f-8559-467385501865";
    public string Name => "Agent Skills";
    public string Description => "Enables Anthropic's Agent Skills standard (progressive disclosure of tools).";
    public string IconUrl => "https://avatars.githubusercontent.com/u/108622152?s=200&v=4";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // 注册设置
        var settings = new AgentSkillsSettings();
        config.Bind("AgentSkills", settings);
        services.AddSingleton(settings);

        // 注册核心服务
        services.AddSingleton<IAgentSkillService, AgentSkillService>();
        
        // 注册 Hook
        services.AddScoped<IAgentHook, AgentSkillHook>();

        // 注册 Function Tools
        services.AddScoped<IFunctionCallback, LoadSkillFn>();
        services.AddScoped<IFunctionCallback, RunSkillScriptFn>();
    }
}
