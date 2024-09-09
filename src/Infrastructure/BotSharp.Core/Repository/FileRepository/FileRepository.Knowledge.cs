using BotSharp.Abstraction.VectorStorage.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool SaveKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var configFile = Path.Combine(dir, COLLECTION_CONFIG_FILE);
        File.WriteAllText(configFile, JsonSerializer.Serialize(configs ?? new(), _options));
        return true;
    }

    public VectorCollectionConfig? GetKnowledgeCollectionConfig(string collectionName)
    {
        throw new NotImplementedException();
    }
}
