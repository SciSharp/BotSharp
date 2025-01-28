using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public BotSharpStats? GetGlobalStats(string metric, string dimension, DateTime recordTime)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var dir = Path.Combine(baseDir, metric, recordTime.Year.ToString(), recordTime.Month.ToString("D2"));
        if (!Directory.Exists(dir)) return null;

        var file = Directory.GetFiles(dir).FirstOrDefault(x => Path.GetFileName(x) == STATS_FILE);
        if (file == null) return null;

        var time = BuildRecordTime(recordTime);
        var text = File.ReadAllText(file);
        var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
        var found = list?.FirstOrDefault(x => x.Metric.IsEqualTo(metric)
                                            && x.Dimension.IsEqualTo(dimension)
                                            && x.RecordTime == time);
        return found;
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var dir = Path.Combine(baseDir, body.Metric, body.RecordTime.Year.ToString(), body.RecordTime.Month.ToString("D2"));
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var file = Path.Combine(dir, STATS_FILE);
        if (!File.Exists(file))
        {
            var list = new List<BotSharpStats> { body };
            File.WriteAllText(file, JsonSerializer.Serialize(list, _options));
        }
        else
        {
            var time = BuildRecordTime(body.RecordTime);
            var text = File.ReadAllText(file);
            var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
            var found = list?.FirstOrDefault(x => x.Metric.IsEqualTo(body.Metric)
                                                && x.Dimension.IsEqualTo(body.Dimension)
                                                && x.RecordTime == time);

            if (found != null)
            {
                found.Metric = body.Metric;
                found.Dimension = body.Dimension;
                found.Data = body.Data;
                found.RecordTime = body.RecordTime;
            }
            else if (list != null)
            {
                list.Add(body);
            }
            else if (list == null)
            {
                list = new List<BotSharpStats> { body };
            }

            File.WriteAllText(file, JsonSerializer.Serialize(list, _options));
        }

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
