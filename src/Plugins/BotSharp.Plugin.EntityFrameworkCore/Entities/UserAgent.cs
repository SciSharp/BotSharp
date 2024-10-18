namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class UserAgent
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string AgentId { get; set; }
    public bool Editable { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
