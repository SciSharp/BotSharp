using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationFileDocument : MongoBase
{
    public string ConversationId { get; set; }
    public string? Thumbnail { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public static ConversationFile ToDomainModel(ConversationFileDocument model)
    {
        return new ConversationFile
        {
            ConversationId = model.ConversationId,
            Thumbnail = model.Thumbnail,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime
        };
    }

    public static ConversationFileDocument ToMongoModel(ConversationFile model)
    {
        return new ConversationFileDocument
        {
            ConversationId = model.ConversationId,
            Thumbnail = model.Thumbnail,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime
        };
    }
}
