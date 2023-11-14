using BotSharp.Abstraction.Conversations.Models;
using BotSharp.OpenAPI.ViewModels.Users;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationViewModel
{
    public string Id { get; set; }
    public string AgentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public UserViewModel User {  get; set; } = new UserViewModel();
    public int UnreadMsgCount { get; set; }
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static ConversationViewModel FromSession(Conversation sess)
    {
        return new ConversationViewModel
        {
            Id = sess.Id,
            User = new UserViewModel 
            { 
                Id = sess.UserId 
            },
            AgentId = sess.AgentId,
            Title = sess.Title,
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }
}
