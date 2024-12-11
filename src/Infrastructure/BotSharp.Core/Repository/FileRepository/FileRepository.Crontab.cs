using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool UpsertCrontabItem(CrontabItem cron)
    {
        if (cron == null || string.IsNullOrWhiteSpace(cron.ConversationId))
        {
            return false;
        }

        try
        {
            var baseDir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, cron.ConversationId);
            if (!Directory.Exists(baseDir))
            {
                return false;
            }

            var cronFile = Path.Combine(baseDir, CRON_FILE);
            var json = JsonSerializer.Serialize(cron, _options);
            File.WriteAllText(cronFile, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when saving crontab item: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public bool DeleteCrontabItem(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return false;
        }

        try
        {
            var baseDir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, conversationId);
            if (!Directory.Exists(baseDir))
            {
                return false;
            }

            var cronFile = Path.Combine(baseDir, CRON_FILE);
            if (!File.Exists(cronFile))
            {
                return false;
            }

            File.Delete(cronFile);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when deleting crontab item (${conversationId}): {ex.Message}\r\n{ex.InnerException}");
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
        var baseDir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        var totalDirs = Directory.GetDirectories(baseDir);
        foreach (var d in totalDirs)
        {
            var file = Path.Combine(d, CRON_FILE);
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
