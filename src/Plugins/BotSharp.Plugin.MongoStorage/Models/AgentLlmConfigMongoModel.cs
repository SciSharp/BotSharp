using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentLlmConfigMongoModel
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public bool IsInherit { get; set; }
    public int MaxRecursionDepth { get; set; }
    public int? MaxOutputTokens { get; set; }
    public string? ReasoningEffortLevel { get; set; }

    public LlmImageCompositionConfigMongoModel? ImageComposition { get; set; }
    public LlmAudioTranscriptionConfigMongoModel? AudioTranscription { get; set; }
    public LlmRealtimeConfigMongoModel? Realtime { get; set; }
    public LlmLiveConfigMongoModel? Live { get; set; }



    public static AgentLlmConfigMongoModel? ToMongoModel(AgentLlmConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new AgentLlmConfigMongoModel
        {
            Provider = config.Provider,
            Model = config.Model,
            IsInherit = config.IsInherit,
            MaxRecursionDepth = config.MaxRecursionDepth,
            MaxOutputTokens = config.MaxOutputTokens,
            ReasoningEffortLevel = config.ReasoningEffortLevel,
            ImageComposition = LlmImageCompositionConfigMongoModel.ToMongoModel(config.ImageComposition),
            AudioTranscription = LlmAudioTranscriptionConfigMongoModel.ToMongoModel(config.AudioTranscription),
            Realtime = LlmRealtimeConfigMongoModel.ToMongoModel(config.Realtime),
            Live = LlmLiveConfigMongoModel.ToMongoModel(config.Live)
        };
    }

    public static AgentLlmConfig? ToDomainModel(AgentLlmConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new AgentLlmConfig
        {
            Provider = config.Provider,
            Model = config.Model,
            IsInherit = config.IsInherit,
            MaxRecursionDepth = config.MaxRecursionDepth,
            MaxOutputTokens = config.MaxOutputTokens,
            ReasoningEffortLevel = config.ReasoningEffortLevel,
            ImageComposition = LlmImageCompositionConfigMongoModel.ToDomainModel(config.ImageComposition),
            AudioTranscription = LlmAudioTranscriptionConfigMongoModel.ToDomainModel(config.AudioTranscription),
            Realtime = LlmRealtimeConfigMongoModel.ToDomainModel(config.Realtime),
            Live = LlmLiveConfigMongoModel.ToDomainModel(config.Live)
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class LlmImageCompositionConfigMongoModel : LlmProviderModelMongoModel
{
    public static LlmImageCompositionConfig? ToDomainModel(LlmImageCompositionConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmImageCompositionConfig
        {
            Provider = config.Provider,
            Model = config.Model
        };
    }

    public static LlmImageCompositionConfigMongoModel? ToMongoModel(LlmImageCompositionConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmImageCompositionConfigMongoModel
        {
            Provider = config.Provider,
            Model = config.Model
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class LlmAudioTranscriptionConfigMongoModel : LlmProviderModelMongoModel
{
    public static LlmAudioTranscriptionConfig? ToDomainModel(LlmAudioTranscriptionConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmAudioTranscriptionConfig
        {
            Provider = config.Provider,
            Model = config.Model
        };
    }

    public static LlmAudioTranscriptionConfigMongoModel? ToMongoModel(LlmAudioTranscriptionConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmAudioTranscriptionConfigMongoModel
        {
            Provider = config.Provider,
            Model = config.Model
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class LlmRealtimeConfigMongoModel : LlmProviderModelMongoModel
{
    public string? ReasoningEffortLevel { get; set; }

    public static LlmRealtimeConfig? ToDomainModel(LlmRealtimeConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmRealtimeConfig
        {
            Provider = config.Provider,
            Model = config.Model,
            ReasoningEffortLevel = config.ReasoningEffortLevel
        };
    }

    public static LlmRealtimeConfigMongoModel? ToMongoModel(LlmRealtimeConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmRealtimeConfigMongoModel
        {
            Provider = config.Provider,
            Model = config.Model,
            ReasoningEffortLevel = config.ReasoningEffortLevel
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class LlmLiveConfigMongoModel : LlmProviderModelMongoModel
{
    public string? ReasoningEffortLevel { get; set; }

    public static LlmLiveConfig? ToDomainModel(LlmLiveConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmLiveConfig
        {
            Provider = config.Provider,
            Model = config.Model,
            ReasoningEffortLevel = config.ReasoningEffortLevel
        };
    }

    public static LlmLiveConfigMongoModel? ToMongoModel(LlmLiveConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new LlmLiveConfigMongoModel
        {
            Provider = config.Provider,
            Model = config.Model,
            ReasoningEffortLevel = config.ReasoningEffortLevel
        };
    }
}
