using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentTemplateMongoElement
{
    public string Name { get; set; } = default!;
    public string Content { get; set; } = string.Empty;
    public AgentTemplateLlmConfigMongoModel? LlmConfig { get; set; }

    public static AgentTemplateMongoElement ToMongoElement(AgentTemplate template)
    {
        return new AgentTemplateMongoElement
        {
            Name = template.Name,
            Content = template.Content,
            LlmConfig = AgentTemplateLlmConfigMongoModel.ToMongoModel(template.LlmConfig)
        };
    }

    public static AgentTemplate ToDomainElement(AgentTemplateMongoElement mongoTemplate)
    {
        return new AgentTemplate
        {
            Name = mongoTemplate.Name,
            Content = mongoTemplate.Content,
            LlmConfig = AgentTemplateLlmConfigMongoModel.ToDomainModel(mongoTemplate.LlmConfig)
        };
    }
}
