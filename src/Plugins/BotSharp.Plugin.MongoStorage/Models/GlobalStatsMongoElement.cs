namespace BotSharp.Plugin.MongoStorage.Models;

public class StatsCountMongoElement
{
    public long AgentCallCount { get; set; }
}

public class StatsLlmCostMongoElement
{
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public float PromptTotalCost { get; set; }
    public float CompletionTotalCost { get; set; }
}