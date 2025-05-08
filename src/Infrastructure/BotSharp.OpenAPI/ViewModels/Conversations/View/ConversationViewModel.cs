using BotSharp.Abstraction.Conversations.Dtos;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationViewModel : ConversationDto
{
    public static ConversationViewModel FromSession(Conversation sess)
    {
        return new ConversationViewModel
        {
            Id = sess.Id,
            User = new() 
            { 
                Id = sess.UserId 
            },
            AgentId = sess.AgentId,
            Title = sess.Title,
            TitleAlias = sess.TitleAlias,
            Channel = sess.Channel,
            Status = sess.Status,
            TaskId = sess.TaskId,
            Tags = sess.Tags ?? [],
            States = sess.States ?? [],
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }
}
