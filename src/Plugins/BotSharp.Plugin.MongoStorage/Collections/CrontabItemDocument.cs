using BotSharp.Abstraction.Crontab.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class CrontabItemDocument : MongoBase
{
    public string UserId { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string ConversationId { get; set; } = default!;
    public string ExecutionResult { get; set; } = default!;
    public string Cron { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int ExecutionCount { get; set; }
    public int MaxExecutionCount { get; set; }
    public int ExpireSeconds { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public bool LessThan60Seconds { get; set; } = false;
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
            ExecutionCount = item.ExecutionCount,
            MaxExecutionCount = item.MaxExecutionCount,
            ExpireSeconds = item.ExpireSeconds,
            LastExecutionTime = item.LastExecutionTime,
            LessThan60Seconds = item.LessThan60Seconds,
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
            ExecutionCount = item.ExecutionCount,
            MaxExecutionCount = item.MaxExecutionCount,
            ExpireSeconds = item.ExpireSeconds,
            LastExecutionTime = item.LastExecutionTime,
            LessThan60Seconds = item.LessThan60Seconds,
            Tasks = item.Tasks?.Select(x => CronTaskMongoElement.ToMongoElement(x))?.ToList() ?? [],
            CreatedTime = item.CreatedTime
        };
    }
}
