using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public BotSharpStats? GetGlobalStats(string category, string group, DateTime recordTime)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var dir = Path.Combine(baseDir, category, recordTime.Year.ToString(), recordTime.Month.ToString("D2"));
        if (!Directory.Exists(dir)) return null;

        var file = Directory.GetFiles(dir).FirstOrDefault(x => Path.GetFileName(x) == STATS_FILE);
        if (file == null) return null;

        var text = File.ReadAllText(file);
        var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
        var found = list?.FirstOrDefault(x => x.Category.IsEqualTo(category)
                                            && x.Group.IsEqualTo(group)
                                            && x.RecordTime == recordTime);
        return found;
    }

    public bool SaveGlobalStats(BotSharpStats body)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var dir = Path.Combine(baseDir, body.Category, body.RecordTime.Year.ToString(), body.RecordTime.Month.ToString("D2"));
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
            var found = list?.FirstOrDefault(x => x.Category.IsEqualTo(body.Category)
                                                && x.Group.IsEqualTo(body.Group)
                                                && x.RecordTime == body.RecordTime);

            if (found != null)
            {
                found.Category = body.Category;
                found.Group = body.Group;
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
}
