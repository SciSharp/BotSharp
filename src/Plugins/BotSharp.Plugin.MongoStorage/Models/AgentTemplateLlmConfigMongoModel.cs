using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class AgentTemplateLlmConfigMongoModel
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int? MaxOutputTokens { get; set; }
    public string? ReasoningEffortLevel { get; set; }

    public static AgentTemplateLlmConfigMongoModel? ToMongoModel(AgentTemplateLlmConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new AgentTemplateLlmConfigMongoModel
        {
            Provider = config.Provider,
            Model = config.Model,
            MaxOutputTokens = config.MaxOutputTokens,
            ReasoningEffortLevel = config.ReasoningEffortLevel
        };
    }

    public static AgentTemplateLlmConfig? ToDomainModel(AgentTemplateLlmConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new AgentTemplateLlmConfig
        {
            Provider = config.Provider,
            Model = config.Model,
            MaxOutputTokens = config.MaxOutputTokens,
            ReasoningEffortLevel = config.ReasoningEffortLevel
        };
    }
}