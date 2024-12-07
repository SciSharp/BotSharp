namespace BotSharp.Core.Crontab.Models;

public class CrontabItem : ScheduleTaskArgs
{
    public string UserId { get; set; } = null!;
    public string AgentId { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public string ExecutionResult { get; set; } = null!;

    public override string ToString()
    {
        return $"{Topic}: {Description} [AgentId: {AgentId}, UserId: {UserId}]";
    }
}
