using System.Collections.Generic;
using System.Threading.Tasks;
using BotSharp.Plugin.AgentSkills.Models;

namespace BotSharp.Plugin.AgentSkills.Services;

public interface IAgentSkillService
{
    Task<List<AgentSkill>> GetAvailableSkills();
    Task<AgentSkill> GetSkill(string name);
    string GetScriptPath(string skillName, string scriptFile);
    Task RefreshSkills();
}
