using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Plugin.AgentSkills.Services;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations;
using System.Text.Json;

namespace BotSharp.Plugin.AgentSkills.Hooks;

public class AgentSkillHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    public AgentSkillHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, IDictionary<string, object> dict)
    {
        if(Agent.Type == AgentType.Routing || Agent.Type == AgentType.Planning)
        {
            return base.OnInstructionLoaded(template, dict);
        }
        var skillService = _services.GetRequiredService<IAgentSkillService>();
        var stateService = _services.GetRequiredService<IConversationStateService>();
        
        // 1. Discovery Phase: Inject Available Skills
        var availableSkills = skillService.GetAvailableSkills().Result; // Sync for hook
        if (availableSkills.Any())
        {
            var skillMenu = "\n\n[Available Agent Skills]\nYou have access to the following specialized skills. If a task requires one, call the 'load_skill' function with the skill name.\n";
            foreach (var skill in availableSkills)
            {
                 skillMenu += $"- {skill.Name}: {skill.Description}\n";
            }
            
            // 将菜单追加到 System Instruction 中
            // 注意：BotSharp 的 OnInstructionLoaded 允许修改 dict 还是 template？
            // 假设我们修改 Agent.Instruction 或追加到 Context
            this.Agent.Instruction += skillMenu;
        }

        // 2. Activation Phase: Inject Active Skills
        var activeSkillsJson = stateService.GetState("active_skills");
        if (!string.IsNullOrEmpty(activeSkillsJson))
        {
             // 简单的 CSV 解析或 Json 解析，视 load_skill 存储格式而定
             // 假设 active_skills 是逗号分隔的字符串
             var activeSkillNames = activeSkillsJson.Split(',', StringSplitOptions.RemoveEmptyEntries);
             
             foreach(var name in activeSkillNames)
             {
                 var skill = skillService.GetSkill(name.Trim()).Result;
                 if (skill != null)
                 {
                     this.Agent.Instruction += $"\n\n### ACTIVE SKILL: {skill.Name.ToUpper()}\n{skill.MarkdownBody}\n";
                 }
             }
        }
        
        return base.OnInstructionLoaded(template, dict);
    }
}
