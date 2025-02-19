using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentResponseMongoElement
{
    public string Prefix { get; set; } = default!;
    public string Intent { get; set; } = default!;
    public string Content { get; set; } = default!;

    public static AgentResponseMongoElement ToMongoElement(AgentResponse response)
    {
        return new AgentResponseMongoElement
        {
            Prefix = response.Prefix,
            Intent = response.Intent,
            Content = response.Content
        };
    }

    public static AgentResponse ToDomainElement(AgentResponseMongoElement mongoResponse)
    {
        return new AgentResponse
        {
            Prefix = mongoResponse.Prefix,
            Intent = mongoResponse.Intent,
            Content = mongoResponse.Content
        };
    }
}
