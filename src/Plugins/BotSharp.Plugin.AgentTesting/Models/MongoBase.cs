using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

/// <summary>
/// Deliberately a local copy of BotSharp.Plugin.MongoStorage's own MongoBase/StringGuidIdGenerator
/// pair rather than a reference to that plugin. Two plugins referencing each other would mean this
/// harness cannot be enabled without also enabling Mongo storage, which is not a real dependency --
/// the test set keeps its own four collections and does not touch BotSharp's storage backend at all.
/// Eight lines of duplication is the cheaper side of that trade.
/// </summary>
[BsonIgnoreExtraElements(Inherited = true)]
public abstract class MongoBase
{
    [BsonId(IdGenerator = typeof(StringGuidIdGenerator))]
    public string Id { get; set; } = default!;
}

public class StringGuidIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document) => Guid.NewGuid().ToString();

    public bool IsEmpty(object id) => id == null || string.IsNullOrEmpty(id.ToString());
}
