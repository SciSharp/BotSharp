using BotSharp.Abstraction.Conversations.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace BotSharp.Core.Repository.Collections;

public class ConversationCollection : IMongoDbCollection
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }

    public string SessionId { get; set; }
    public string UserId { get; set; }
    public string Model { get; set; }

    public string Title { get; set; }
    public List<RoleDialogModel> Messages { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
