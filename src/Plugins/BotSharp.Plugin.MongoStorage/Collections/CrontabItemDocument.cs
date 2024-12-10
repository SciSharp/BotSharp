using BotSharp.Abstraction.Crontab.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class CrontabItemDocument : MongoBase
{
    public string UserId { get; set; }
    public string AgentId { get; set; }
    public string ConversationId { get; set; }
    public string ExecutionResult { get; set; }
    public string Cron { get; set; }
    public string Topic { get; set; }
    public string Description { get; set; }
    public string Script { get; set; }
    public string Language { get; set; }
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
            Topic = item.Topic,
            Description = item.Description,
            Script = item.Script,
            Language = item.Language,
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
            Topic = item.Topic,
            Description = item.Description,
            Script = item.Script,
            Language = item.Language,
            CreatedTime = item.CreatedTime
        };
    }
}
