using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public BotSharpStats? GetGlobalStats(string metric, string dimension, DateTime recordTime)
    {
        var time = BuildRecordTime(recordTime);

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Metric, metric),
            builder.Eq(x => x.Dimension, dimension),
            builder.Eq(x => x.RecordTime, time)
        };

        var filterDef = builder.And(filters);
        var found = _dc.GlobalStatistics.Find(filterDef).FirstOrDefault();
        if (found == null) return null;

        return new BotSharpStats
        {
            Metric = found.Metric,
            Dimension = found.Dimension,
            Data = found.Data,
            RecordTime = found.RecordTime
        };
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var time = BuildRecordTime(body.RecordTime);
        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Metric, body.Metric),
            builder.Eq(x => x.Dimension, body.Dimension),
            builder.Eq(x => x.RecordTime, time)
        };

        var filterDef = builder.And(filters);
        var updateDef = Builders<GlobalStatisticsDocument>.Update
                            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                            .Set(x => x.Metric, body.Metric)
                            .Set(x => x.Dimension, body.Dimension)
                            .Set(x => x.Data, body.Data)
                            .Set(x => x.RecordTime, time);

        _dc.GlobalStatistics.UpdateOne(filterDef, updateDef, _options);
        return true;
    }

    #region Private methods
    private DateTime BuildRecordTime(DateTime date)
    {
        var recordDate = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
        return DateTime.SpecifyKind(recordDate, DateTimeKind.Utc);
    }
    #endregion
}
