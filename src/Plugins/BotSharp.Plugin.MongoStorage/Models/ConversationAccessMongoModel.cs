using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class ConversationAccessMongoModel
{
    public string AccessLevel { get; set; } = string.Empty;
    public string AccessBy { get; set; } = string.Empty;
    public IEnumerable<string> Accessors { get; set; } = new List<string>();

    public static ConversationAccessMongoModel ToMongoModel(ConversationAccess? model)
    {
        if (model == null)
        {
            return new();
        }

        return new ConversationAccessMongoModel
        {
            AccessLevel = model.AccessLevel,
            AccessBy = model.AccessBy,
            Accessors = model.Accessors
        };
    }

    public static ConversationAccess ToDomainModel(ConversationAccessMongoModel? model)
    {
        if (model == null)
        {
            return new();
        }

        return new ConversationAccess
        {
            AccessLevel = model.AccessLevel,
            AccessBy = model.AccessBy,
            Accessors = model.Accessors
        };
    }
}
