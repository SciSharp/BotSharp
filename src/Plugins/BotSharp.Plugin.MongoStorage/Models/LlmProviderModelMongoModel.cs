using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class LlmProviderModelMongoModel
{
    public string? Provider { get; set; }
    public string? Model { get; set; }

    public static LlmProviderModel? ToDomainModel(LlmProviderModelMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmProviderModel
        {
            Provider = config.Provider,
            Model = config.Model,
        };
    }

    public static LlmProviderModelMongoModel? ToMongoModel(LlmProviderModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmProviderModelMongoModel
        {
            Provider = config.Provider,
            Model = config.Model,
        };
    }
}
