using BotSharp.Abstraction.Statistics.Enums;
using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public BotSharpStats? GetGlobalStats(string metric, string dimension, string dimRefVal, DateTime recordTime, StatsInterval interval)
    {
        var (startTime, endTime) = BotSharpStats.BuildTimeInterval(recordTime, interval);

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Metric, metric),
            builder.Eq(x => x.Dimension, dimension),
            builder.Eq(x => x.DimRefVal, dimRefVal),
            builder.Eq(x => x.StartTime, startTime),
            builder.Eq(x => x.EndTime, endTime)
        };

        var filterDef = builder.And(filters);
        var found = _dc.GlobalStatistics.Find(filterDef).FirstOrDefault();
        if (found == null) return null;

        return new BotSharpStats
        {
            Metric = found.Metric,
            Dimension = found.Dimension,
            DimRefVal = found.DimRefVal,
            Data = found.Data,
            RecordTime = found.RecordTime,
            StartTime = startTime,
            EndTime = endTime,
            Interval = interval.ToString()
        };
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var (startTime, endTime) = BotSharpStats.BuildTimeInterval(body.RecordTime, body.IntervalType);
        body.RecordTime = DateTime.SpecifyKind(body.RecordTime, DateTimeKind.Utc);
        body.StartTime = startTime;
        body.EndTime = endTime;

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Metric, body.Metric),
            builder.Eq(x => x.Dimension, body.Dimension),
            builder.Eq(x => x.DimRefVal, body.DimRefVal),
            builder.Eq(x => x.StartTime, startTime),
            builder.Eq(x => x.EndTime, endTime)
        };

        var filterDef = builder.And(filters);
        var updateDef = Builders<GlobalStatisticsDocument>.Update
                            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                            .Set(x => x.Metric, body.Metric)
                            .Set(x => x.Dimension, body.Dimension)
                            .Set(x => x.DimRefVal, body.DimRefVal)
                            .Set(x => x.Data, body.Data)
                            .Set(x => x.StartTime, body.StartTime)
                            .Set(x => x.EndTime, body.EndTime)
                            .Set(x => x.Interval, body.Interval)
                            .Set(x => x.RecordTime, body.RecordTime);

        _dc.GlobalStatistics.UpdateOne(filterDef, updateDef, _options);
        return true;
    }
}
