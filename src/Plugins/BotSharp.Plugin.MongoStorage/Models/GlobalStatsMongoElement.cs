namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class StatsCountMongoElement
{
    public long AgentCallCount { get; set; }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class StatsLlmCostMongoElement
{
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public float PromptTotalCost { get; set; }
    public float CompletionTotalCost { get; set; }
}