using AgentSkillsDotNet;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.AgentSkills.Functions;
using BotSharp.Plugin.AgentSkills.Hooks;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AgentSkills;

/// <summary>
/// Agent Skills plugin for BotSharp.
/// Enables AI agents to leverage reusable skills following the Agent Skills specification (https://agentskills.io).
/// Implements requirements: FR-1.1, FR-3.1, FR-4.1
/// </summary>
public class AgentSkillsPlugin : IBotSharpPlugin
{
    public string Id => "a5b3e8c1-7d2f-4a9e-b6c4-8f5d1e2a3b4c";
    public string Name => "Agent Skills";
    public string Description => "Enables AI agents to leverage reusable skills following the Agent Skills specification (https://agentskills.io).";
    public string IconUrl => "https://raw.githubusercontent.com/SciSharp/BotSharp/master/docs/static/logos/BotSharp.png";
    public string[] AgentIds => [];

    /// <summary>
    /// Register dependency injection services.
    /// Implements requirements: FR-1.1, FR-3.1, FR-4.1, FR-6.1, NFR-4.1
    /// </summary>
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // FR-6.1: Register AgentSkillsSettings configuration
        // Use ISettingService to bind configuration from appsettings.json
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AgentSkillsSettings>("AgentSkills");
        });

        // FR-1.1: Register AgentSkillsFactory as singleton
        // Singleton pattern avoids creating multiple factory instances
        services.AddSingleton<AgentSkillsFactory>();

        // FR-1.1, NFR-4.1: Register ISkillService and SkillService as scoped 
        services.AddScoped<ISkillService, SkillService>();

        // FR-4.1: Register skill tools as IFunctionCallback
        // Build temporary service provider to access ISkillService
        using (var sp = services.BuildServiceProvider())
        {
            try
            {
                var logger = sp.GetService<ILogger<AgentSkillsPlugin>>();
                logger?.LogInformation("Registering Agent Skills tools...");

                var skillService = sp.GetRequiredService<ISkillService>();
                var tools = skillService.GetTools();

                logger?.LogInformation("Found {ToolCount} tools from {SkillCount} skills",
                    tools.Count, skillService.GetSkillCount());

                // FR-4.1: Register each AIFunction as IFunctionCallback
                foreach (var tool in tools)
                {
                    if (tool is AIFunction aiFunc)
                    {
                        // Capture aiFunc in closure to avoid reference issues
                        var capturedFunc = aiFunc;
                        services.AddSingleton<AIFunction>(capturedFunc);
                        // Register as Scoped - new instance per request to avoid state sharing
                        services.AddScoped<IFunctionCallback>(provider =>
                            new AIToolCallbackAdapter(
                                capturedFunc,
                                provider,
                                provider.GetService<ILogger<AIToolCallbackAdapter>>()));

                        logger?.LogDebug("Registered tool: {ToolName}", aiFunc.Name);
                    }
                }

                logger?.LogInformation("Successfully registered {ToolCount} Agent Skills tools", tools.Count);
            }
            catch (Exception ex)
            {
                // FR-1.3: Log error but don't interrupt application startup
                var logger = sp.GetService<ILogger<AgentSkillsPlugin>>();
                logger?.LogError(ex, "Failed to register Agent Skills tools. Plugin will continue with limited functionality.");
            }
        }

        // FR-2.1: Register AgentSkillsInstructionHook for instruction injection
        services.AddScoped<IAgentHook, AgentSkillsInstructionHook>();

        // FR-3.1: Register AgentSkillsFunctionHook for function registration
        services.AddScoped<IAgentHook, AgentSkillsFunctionHook>();
    }
}
