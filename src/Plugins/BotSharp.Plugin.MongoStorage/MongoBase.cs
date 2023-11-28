namespace BotSharp.Plugin.MongoStorage;

[BsonIgnoreExtraElements(Inherited = true)]
public abstract class MongoBase
{
    [BsonId(IdGenerator = typeof(StringGuidIdGenerator))]
    public string Id { get; set; }
}


