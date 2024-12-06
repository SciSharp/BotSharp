namespace BotSharp.Core.Crontab.Models;

public class CrontabItem
{
    public string UserId { get; set; } = null!;
    public string AgentId { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public string Cron { get; set; } = null!;

    public override string ToString()
    {
        return $"AgentId: {AgentId}, UserId: {UserId}, Topic: {Topic}";
    }
}
