using BotSharp.Abstraction.Statistics.Enums;
using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public BotSharpStats? GetGlobalStats(string metric, string dimension, DateTime recordTime, StatsInterval interval)
    {
        var (startTime, endTime) = BuildTimeInterval(recordTime, interval);

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Metric, metric),
            builder.Eq(x => x.Dimension, dimension),
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
            Data = found.Data,
            RecordTime = found.RecordTime,
            StartTime = startTime,
            EndTime = endTime,
            Interval = interval.ToString()
        };
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var (startTime, endTime) = BuildTimeInterval(body.RecordTime, body.IntervalType);
        body.RecordTime = DateTime.SpecifyKind(body.RecordTime, DateTimeKind.Utc);
        body.StartTime = startTime;
        body.EndTime = endTime;

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Metric, body.Metric),
            builder.Eq(x => x.Dimension, body.Dimension),
            builder.Eq(x => x.StartTime, startTime),
            builder.Eq(x => x.EndTime, endTime)
        };

        var filterDef = builder.And(filters);
        var updateDef = Builders<GlobalStatisticsDocument>.Update
                            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                            .Set(x => x.Metric, body.Metric)
                            .Set(x => x.Dimension, body.Dimension)
                            .Set(x => x.Data, body.Data)
                            .Set(x => x.StartTime, body.StartTime)
                            .Set(x => x.EndTime, body.EndTime)
                            .Set(x => x.Interval, body.Interval)
                            .Set(x => x.RecordTime, body.RecordTime);

        _dc.GlobalStatistics.UpdateOne(filterDef, updateDef, _options);
        return true;
    }

    #region Private methods
    private (DateTime, DateTime) BuildTimeInterval(DateTime recordTime, StatsInterval interval)
    {
        DateTime startTime = recordTime;
        DateTime endTime = DateTime.UtcNow;

        switch (interval)
        {
            case StatsInterval.Hour:
                startTime = new DateTime(recordTime.Year, recordTime.Month, recordTime.Day, recordTime.Hour, 0, 0);
                endTime = startTime.AddHours(1);
                break;
            case StatsInterval.Week:
                var dayOfWeek = startTime.DayOfWeek;
                var firstDayOfWeek = startTime.AddDays(-(int)dayOfWeek);
                startTime = new DateTime(firstDayOfWeek.Year, firstDayOfWeek.Month, firstDayOfWeek.Day, 0, 0, 0);
                endTime = startTime.AddDays(7);
                break;
            case StatsInterval.Month:
                startTime = new DateTime(recordTime.Year, recordTime.Month, 1);
                endTime = startTime.AddMonths(1);
                break;
            default:
                startTime = new DateTime(recordTime.Year, recordTime.Month, recordTime.Day, 0, 0, 0);
                endTime = startTime.AddDays(1);
                break;
        }

        startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
        endTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc);
        return (startTime, endTime);
    }
    #endregion
}
