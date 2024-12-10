using BotSharp.Abstraction.Crontab.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool InsertCrontabItem(CrontabItem item)
    {
        if (item == null)
        {
            return false;
        }

        try
        {
            var baseDir = Path.Combine(_dbSettings.FileRepository, CRONTAB_FOLDER);
            item.Id = Guid.NewGuid().ToString();
            var dir = Path.Combine(baseDir, item.Id);

            if (Directory.Exists(dir))
            {
                return false;
            }

            Directory.CreateDirectory(dir);
            Thread.Sleep(50);

            var itemFile = Path.Combine(dir, CRONTAB_FILE);
            var json = JsonSerializer.Serialize(item, _options);
            File.WriteAllText(itemFile, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when saving crontab item: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }


    public PagedItems<CrontabItem> GetCrontabItems(CrontabItemFilter filter)
    {
        
        if (filter == null)
        {
            filter = CrontabItemFilter.Empty();
        }

        var records = new List<CrontabItem>();
        var dir = Path.Combine(_dbSettings.FileRepository, CRONTAB_FOLDER);

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var totalDirs = Directory.GetDirectories(dir);
        foreach (var d in totalDirs)
        {
            var file = Path.Combine(d, CRONTAB_FILE);
            if (!File.Exists(file)) continue;

            var json = File.ReadAllText(file);
            var record = JsonSerializer.Deserialize<CrontabItem>(json, _options);
            if (record == null) continue;

            var matched = true;
            if (filter?.AgentIds != null)
            {
                matched = matched && filter.AgentIds.Contains(record.AgentId);
            }
            if (filter?.ConversationIds != null)
            {
                matched = matched && filter.ConversationIds.Contains(record.ConversationId);
            }
            if (filter?.UserIds != null)
            {
                matched = matched && filter.UserIds.Contains(record.UserId);
            }
            if (filter?.Titles != null)
            {
                matched = matched && filter.Titles.Contains(record.Title);
            }

            if (!matched) continue;

            records.Add(record);
        }

        return new PagedItems<CrontabItem>
        {
            Items = records.OrderByDescending(x => x.CreatedTime).Skip(filter.Offset).Take(filter.Size),
            Count = records.Count(),
        };
    }
}
