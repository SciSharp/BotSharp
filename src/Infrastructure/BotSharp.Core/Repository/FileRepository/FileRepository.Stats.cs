using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public BotSharpStats? GetGlobalStats(string metric, string dimension, DateTime recordTime, StatsInterval interval)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var (startTime, endTime) = BuildTimeInterval(recordTime, interval);
        var dir = Path.Combine(baseDir, metric, startTime.Year.ToString(), startTime.Month.ToString("D2"));
        if (!Directory.Exists(dir)) return null;

        var file = Directory.GetFiles(dir).FirstOrDefault(x => Path.GetFileName(x) == STATS_FILE);
        if (file == null) return null;

        var text = File.ReadAllText(file);
        var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
        var found = list?.FirstOrDefault(x => x.Metric.IsEqualTo(metric)
                                            && x.Dimension.IsEqualTo(dimension)
                                            && x.StartTime == startTime
                                            && x.EndTime == endTime);

        return found;
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var (startTime, endTime) = BuildTimeInterval(body.RecordTime, body.IntervalType);
        body.StartTime = startTime;
        body.EndTime = endTime;

        var dir = Path.Combine(baseDir, body.Metric, startTime.Year.ToString(), startTime.Month.ToString("D2"));
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
            var text = File.ReadAllText(file);
            var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
            var found = list?.FirstOrDefault(x => x.Metric.IsEqualTo(body.Metric)
                                                && x.Dimension.IsEqualTo(body.Dimension)
                                                && x.StartTime == startTime
                                                && x.EndTime == endTime);

            if (found != null)
            {
                found.Metric = body.Metric;
                found.Dimension = body.Dimension;
                found.Data = body.Data;
                found.RecordTime = body.RecordTime;
                found.StartTime = body.StartTime;
                found.EndTime = body.EndTime;
                found.Interval = body.Interval;
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
