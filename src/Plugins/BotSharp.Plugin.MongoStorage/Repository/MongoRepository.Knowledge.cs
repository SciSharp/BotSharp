using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool SaveKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs) =>
        throw new NotImplementedException();

    public VectorCollectionConfig? GetKnowledgeCollectionConfig(string collectionName) =>
        throw new NotImplementedException();
}
