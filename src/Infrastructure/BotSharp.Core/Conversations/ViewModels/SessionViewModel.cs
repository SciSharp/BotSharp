using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Core.Conversations.ViewModels;

public class SessionViewModel
{
    public string Id { get; set; }
    public string AgentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static SessionViewModel FromSession(Session sess)
    {
        return new SessionViewModel
        {
            Id = sess.Id,
            AgentId = sess.AgentId,
            Title = sess.Title,
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }
}
