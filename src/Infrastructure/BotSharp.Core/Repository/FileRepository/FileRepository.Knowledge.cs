using BotSharp.Abstraction.VectorStorage.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var configFile = Path.Combine(dir, COLLECTION_CONFIG_FILE);
        if (reset)
        {
            File.WriteAllText(configFile, JsonSerializer.Serialize(configs ?? new(), _options));
            return true;
        }

        if (!File.Exists(configFile))
        {
            File.Create(configFile);
        }

        var str = File.ReadAllText(configFile);
        var savedConfigs = JsonSerializer.Deserialize<List<VectorCollectionConfig>>(str, _options) ?? new();
        savedConfigs.AddRange(configs);
        File.WriteAllText(configFile, JsonSerializer.Serialize(savedConfigs ?? new(), _options));

        return true;
    }

    public bool DeleteKnowledgeCollectionConfig(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return false;

        var configFile = Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER, COLLECTION_CONFIG_FILE);
        if (!File.Exists(configFile)) return false;

        var str = File.ReadAllText(configFile);
        var savedConfigs = JsonSerializer.Deserialize<List<VectorCollectionConfig>>(str, _options) ?? new();
        savedConfigs = savedConfigs.Where(x => x.Name != collectionName).ToList();
        File.WriteAllText(configFile, JsonSerializer.Serialize(savedConfigs ?? new(), _options));

        return true;
    }

    public IEnumerable<VectorCollectionConfig> GetKnowledgeCollectionConfigs(VectorCollectionConfigFilter filter)
    {
        if (filter == null)
        {
            return Enumerable.Empty<VectorCollectionConfig>();
        }

        var file = Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER, COLLECTION_CONFIG_FILE);
        if (!File.Exists(file))
        {
            return Enumerable.Empty<VectorCollectionConfig>();
        }

        var str = File.ReadAllText(file);
        var configs = JsonSerializer.Deserialize<List<VectorCollectionConfig>>(str, _options) ?? new();

        if (!filter.CollectionNames.IsNullOrEmpty())
        {
            configs = configs.Where(x => filter.CollectionNames.Contains(x.Name)).ToList();
        }

        if (!filter.CollectionTypes.IsNullOrEmpty())
        {
            configs = configs.Where(x => filter.CollectionTypes.Contains(x.Type)).ToList();
        }

        return configs;
    }
}
