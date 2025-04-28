using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentLinkMongoElement
{
    public string Name { get; set; } = default!;
    public string Content { get; set; } = default!;

    public static AgentLinkMongoElement ToMongoElement(AgentLink link)
    {
        return new AgentLinkMongoElement
        {
            Name = link.Name,
            Content = link.Content
        };
    }

    public static AgentLink ToDomainElement(AgentLinkMongoElement mongoLink)
    {
        return new AgentLink
        {
            Name = mongoLink.Name,
            Content = mongoLink.Content
        };
    }
}
