using AgentSkillsDotNet;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AgentSkills.Hooks;

/// <summary>
/// Hook that adds Agent Skills utilities to agents.
/// </summary>
public class AgentSkillsConversationHook : ConversationHookBase
{
    private readonly AgentSkillsFactory _skillLoader;
    private readonly AgentSkillsSettings _options;

    public AgentSkillsConversationHook(IServiceProvider services, AgentSettings settings)        
    {
        _skillLoader = services.GetRequiredService<AgentSkillsFactory>();
        _options = services.GetRequiredService<AgentSkillsSettings>();
    } 

}
