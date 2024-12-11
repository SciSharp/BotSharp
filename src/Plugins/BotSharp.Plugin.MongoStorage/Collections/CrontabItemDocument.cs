using BotSharp.Abstraction.Crontab.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class CrontabItemDocument : MongoBase
{
    public string UserId { get; set; }
    public string AgentId { get; set; }
    public string ConversationId { get; set; }
    public string ExecutionResult { get; set; }
    public string Cron { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public IEnumerable<CronTaskMongoElement> Tasks { get; set; } = [];
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static CrontabItem ToDomainModel(CrontabItemDocument item)
    {
        return new CrontabItem
        {
            UserId = item.UserId,
            AgentId = item.AgentId,
            ConversationId = item.ConversationId,
            ExecutionResult = item.ExecutionResult,
            Cron = item.Cron,
            Title = item.Title,
            Description = item.Description,
            Tasks = item.Tasks?.Select(x => CronTaskMongoElement.ToDomainElement(x))?.ToArray() ?? [],
            CreatedTime = item.CreatedTime
        };
    }

    public static CrontabItemDocument ToMongoModel(CrontabItem item)
    {
        return new CrontabItemDocument
        {
            UserId = item.UserId,
            AgentId = item.AgentId,
            ConversationId = item.ConversationId,
            ExecutionResult = item.ExecutionResult,
            Cron = item.Cron,
            Title = item.Title,
            Description = item.Description,
            Tasks = item.Tasks?.Select(x => CronTaskMongoElement.ToMongoElement(x))?.ToList() ?? [],
            CreatedTime = item.CreatedTime
        };
    }
}
