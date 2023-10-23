using MongoDB.Bson.Serialization;

namespace BotSharp.Plugin.MongoStorage;

public class StringGuidIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
    {
        return Guid.NewGuid().ToString();
    }

    public bool IsEmpty(object id)
    {
        return id == null || string.IsNullOrEmpty(id.ToString());
    }
}
