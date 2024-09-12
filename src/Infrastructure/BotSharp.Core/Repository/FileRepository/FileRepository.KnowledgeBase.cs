using BotSharp.Abstraction.VectorStorage.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
    {
        var vectorDir = BuildKnowledgeCollectionConfigDir();
        if (!Directory.Exists(vectorDir))
        {
            Directory.CreateDirectory(vectorDir);
        }

        var configFile = Path.Combine(vectorDir, COLLECTION_CONFIG_FILE);
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

        // Update if collection already exists, otherwise insert
        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.Name)) continue;

            var found = savedConfigs.FirstOrDefault(x => x.Name == config.Name);
            if (found != null)
            {
                found.TextEmbedding = config.TextEmbedding;
                found.Type = config.Type;
            }
            else
            {
                savedConfigs.Add(config);
            }
        }

        File.WriteAllText(configFile, JsonSerializer.Serialize(savedConfigs ?? new(), _options));

        return true;
    }

    public bool DeleteKnowledgeCollectionConfig(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return false;

        var vectorDir = BuildKnowledgeCollectionConfigDir();
        var configFile = Path.Combine(vectorDir, COLLECTION_CONFIG_FILE);
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

        var vectorDir = BuildKnowledgeCollectionConfigDir();
        var configFile = Path.Combine(vectorDir, COLLECTION_CONFIG_FILE);
        if (!File.Exists(configFile))
        {
            return Enumerable.Empty<VectorCollectionConfig>();
        }

        // Get data
        var content = File.ReadAllText(configFile);
        var configs = JsonSerializer.Deserialize<List<VectorCollectionConfig>>(content, _options) ?? new();

        // Apply filters
        if (!filter.CollectionNames.IsNullOrEmpty())
        {
            configs = configs.Where(x => filter.CollectionNames.Contains(x.Name)).ToList();
        }

        if (!filter.CollectionTypes.IsNullOrEmpty())
        {
            configs = configs.Where(x => filter.CollectionTypes.Contains(x.Type)).ToList();
        }

        if (!filter.VectorStroageProviders.IsNullOrEmpty())
        {
            configs = configs.Where(x => filter.VectorStroageProviders.Contains(x.VectorStore?.Provider)).ToList();
        }

        return configs;
    }

    #region Private methods
    private string BuildKnowledgeCollectionConfigDir()
    {
        return Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER);
    }
    #endregion
}
