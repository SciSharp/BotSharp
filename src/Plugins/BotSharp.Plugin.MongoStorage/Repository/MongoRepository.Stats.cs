using BotSharp.Abstraction.Statistics.Enums;
using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public BotSharpStats? GetGlobalStats(string agentId, DateTime recordTime, StatsInterval interval)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return null;
        }

        var (startTime, endTime) = BotSharpStats.BuildTimeInterval(recordTime, interval);

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.AgentId, agentId),
            builder.Eq(x => x.StartTime, startTime),
            builder.Eq(x => x.EndTime, endTime)
        };

        var filterDef = builder.And(filters);
        var found = _dc.GlobalStats.Find(filterDef).FirstOrDefault();

        return found != null ? new BotSharpStats
        {
            AgentId = agentId,
            Count = new()
            {
                AgentCallCount = found.Count.AgentCallCount
            },
            LlmCost = new()
            {
                PromptTokens = found.LlmCost.PromptTokens,
                CompletionTokens = found.LlmCost.CompletionTokens,
                PromptTotalCost = found.LlmCost.PromptTotalCost,
                CompletionTotalCost = found.LlmCost.CompletionTotalCost
            },
            RecordTime = found.RecordTime,
            StartTime = startTime,
            EndTime = endTime,
            Interval = interval.ToString()
        } : null;
    }

    public bool SaveGlobalStats(BotSharpStatsDelta delta)
    {
        if (delta == null || string.IsNullOrWhiteSpace(delta.AgentId))
        {
            return false;
        }

        var (startTime, endTime) = BotSharpStats.BuildTimeInterval(delta.RecordTime, delta.IntervalType);
        delta.RecordTime = DateTime.SpecifyKind(delta.RecordTime, DateTimeKind.Utc);

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.AgentId, delta.AgentId),
            builder.Eq(x => x.StartTime, startTime),
            builder.Eq(x => x.EndTime, endTime)
        };

        var filterDef = builder.And(filters);
        var updateDef = Builders<GlobalStatisticsDocument>.Update
                            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                            .Inc(x => x.Count.AgentCallCount, delta.CountDelta.AgentCallCountDelta)
                            .Inc(x => x.LlmCost.PromptTokens, delta.LlmCostDelta.PromptTokensDelta)
                            .Inc(x => x.LlmCost.CompletionTokens, delta.LlmCostDelta.CompletionTokensDelta)
                            .Inc(x => x.LlmCost.PromptTotalCost, delta.LlmCostDelta.PromptTotalCostDelta)
                            .Inc(x => x.LlmCost.CompletionTotalCost, delta.LlmCostDelta.CompletionTotalCostDelta)
                            .Set(x => x.StartTime, startTime)
                            .Set(x => x.EndTime, endTime)
                            .Set(x => x.Interval, delta.Interval)
                            .Set(x => x.RecordTime, delta.RecordTime);

        _dc.GlobalStats.UpdateOne(filterDef, updateDef, _options);
        return true;
    }
}
