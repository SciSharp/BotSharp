using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public BotSharpStats? GetGlobalStats(string category, string group, DateTime recordDate)
    {
        var date = BuildRecordDate(recordDate);

        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Category, category),
            builder.Eq(x => x.Group, group),
            builder.Eq(x => x.RecordDate, date)
        };

        var filterDef = builder.And(filters);
        var found = _dc.GlobalStatistics.Find(filterDef).FirstOrDefault();
        if (found == null) return null;

        return new BotSharpStats
        {
            Category = found.Category,
            Group = found.Group,
            Data = found.Data,
            RecordDate = found.RecordDate,
        };
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var date = BuildRecordDate(body.RecordDate);
        var builder = Builders<GlobalStatisticsDocument>.Filter;
        var filters = new List<FilterDefinition<GlobalStatisticsDocument>>()
        {
            builder.Eq(x => x.Category, body.Category),
            builder.Eq(x => x.Group, body.Group),
            builder.Eq(x => x.RecordDate, date)
        };

        var filterDef = builder.And(filters);
        var updateDef = Builders<GlobalStatisticsDocument>.Update
                            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                            .Set(x => x.Category, body.Category)
                            .Set(x => x.Group, body.Group)
                            .Set(x => x.Data, body.Data)
                            .Set(x => x.RecordDate, date);

        _dc.GlobalStatistics.UpdateOne(filterDef, updateDef, _options);
        return true;
    }

    #region Private methods
    private DateTime BuildRecordDate(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
    }
    #endregion
}
