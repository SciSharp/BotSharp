using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentKnowledgeBaseMongoElement
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public bool Disabled { get; set; }
    public decimal? Confidence { get; set; }

    public static AgentKnowledgeBaseMongoElement ToMongoElement(AgentKnowledgeBase knowledgeBase)
    {
        return new AgentKnowledgeBaseMongoElement
        {
            Name = knowledgeBase.Name,
            Type = knowledgeBase.Type,
            Disabled = knowledgeBase.Disabled,
            Confidence = knowledgeBase.Confidence
        };
    }

    public static AgentKnowledgeBase ToDomainElement(AgentKnowledgeBaseMongoElement knowledgeBase)
    {
        return new AgentKnowledgeBase
        {
            Name = knowledgeBase.Name,
            Type = knowledgeBase.Type,
            Disabled = knowledgeBase.Disabled,
            Confidence = knowledgeBase.Confidence
        };
    }
}
