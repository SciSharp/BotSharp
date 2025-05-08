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
        var found = _dc.GlobalStatistics.Find(filterDef).FirstOrDefault();

        return found != null ? new BotSharpStats
        {
            AgentId = agentId,
            AgentCallCount = found.AgentCallCount,
            PromptTokens = found.PromptTokens,
            CompletionTokens = found.CompletionTokens,
            PromptTotalCost = found.PromptTotalCost,
            CompletionTotalCost = found.CompletionTotalCost,
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
                            .Inc(x => x.AgentCallCount, delta.AgentCallCountDelta)
                            .Inc(x => x.PromptTokens, delta.PromptTokensDelta)
                            .Inc(x => x.CompletionTokens, delta.CompletionTokensDelta)
                            .Inc(x => x.PromptTotalCost, delta.PromptTotalCostDelta)
                            .Inc(x => x.CompletionTotalCost, delta.CompletionTotalCostDelta)
                            .Set(x => x.StartTime, startTime)
                            .Set(x => x.EndTime, endTime)
                            .Set(x => x.Interval, delta.Interval)
                            .Set(x => x.RecordTime, delta.RecordTime);

        _dc.GlobalStatistics.UpdateOne(filterDef, updateDef, _options);
        return true;
    }
}
