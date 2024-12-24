using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class AgentKnowledgeBaseMongoElement
{
    public string Name { get; set; }
    public bool Disabled { get; set; }
    public static AgentKnowledgeBaseMongoElement ToMongoElement(AgentKnowledgeBase knowledgeBase)
    {
        return new AgentKnowledgeBaseMongoElement
        {
            Name = knowledgeBase.Name ?? string.Empty,
            Disabled = knowledgeBase.Disabled,
        };
    }

    public static AgentKnowledgeBase ToDomainElement(AgentKnowledgeBaseMongoElement knowledgeBase)
    {
        return new AgentKnowledgeBase
        {
            Name = knowledgeBase.Name,
            Disabled = knowledgeBase.Disabled
        };
    }
}
