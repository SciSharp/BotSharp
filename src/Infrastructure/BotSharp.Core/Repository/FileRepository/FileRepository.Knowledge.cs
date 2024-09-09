using BotSharp.Abstraction.VectorStorage.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool ResetKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs)
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
        if (string.IsNullOrWhiteSpace(collectionName)) return null;

        var file = Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER, COLLECTION_CONFIG_FILE);
        if (!File.Exists(file)) return null;

        var str = File.ReadAllText(file);
        var configs = JsonSerializer.Deserialize<List<VectorCollectionConfig>>(str, _options) ?? new();
        return configs.FirstOrDefault(x => x.Name == collectionName);
    }
}
