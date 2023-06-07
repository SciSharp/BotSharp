using MongoDB.Bson;

namespace BotSharp.Core.Repository;

public interface IMongoDbCollection
{
    ObjectId Id { get; set; }
}
