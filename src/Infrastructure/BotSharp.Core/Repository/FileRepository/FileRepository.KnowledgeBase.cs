using BotSharp.Abstraction.VectorStorage.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    #region Configs
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
    #endregion


    #region Documents
    public bool SaveKnolwedgeBaseFileMeta(KnowledgeDocMetaData metaData)
    {
        if (metaData == null
            || string.IsNullOrWhiteSpace(metaData.Collection)
            || string.IsNullOrWhiteSpace(metaData.VectorStoreProvider))
        {
            return false;
        }

        var dir = BuildKnowledgeCollectionFileDir(metaData.Collection, metaData.VectorStoreProvider);
        var docDir = Path.Combine(dir, metaData.FileId.ToString());
        if (!Directory.Exists(docDir))
        {
            Directory.CreateDirectory(docDir);
        }

        var metaFile = Path.Combine(docDir, KNOWLEDGE_DOC_META_FILE);
        var content = JsonSerializer.Serialize(metaData, _options);
        File.WriteAllText(metaFile, content);
        return true;
    }

    public bool DeleteKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, Guid? fileId = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider))
        {
            return false;
        }

        var dir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
        if (!Directory.Exists(dir)) return false;

        if (fileId == null)
        {
            Directory.Delete(dir, true);
        }
        else
        {
            var fileDir = Path.Combine(dir, fileId.ToString());
            if (Directory.Exists(fileDir))
            {
                Directory.Delete(fileDir, true);
            }
        }

        return true;
    }

    public PagedItems<KnowledgeDocMetaData> GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, KnowledgeFileFilter filter)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider))
        {
            return new PagedItems<KnowledgeDocMetaData>();
        }

        var dir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
        if (!Directory.Exists(dir))
        {
            return new PagedItems<KnowledgeDocMetaData>();
        }

        var records = new List<KnowledgeDocMetaData>();
        foreach (var folder in Directory.GetDirectories(dir))
        {
            var metaFile = Path.Combine(folder, KNOWLEDGE_DOC_META_FILE);
            if (!File.Exists(metaFile)) continue;

            var content = File.ReadAllText(metaFile);
            var metaData = JsonSerializer.Deserialize<KnowledgeDocMetaData>(content, _options);
            if (metaData == null) continue;

            var matched = true;

            // Apply filter
            if (filter != null)
            {
                if (!filter.FileIds.IsNullOrEmpty())
                {
                    matched = matched && filter.FileIds.Contains(metaData.FileId);
                }

                if (!filter.FileNames.IsNullOrEmpty())
                {
                    matched = matched && filter.FileNames.Contains(metaData.FileName);
                }

                if (!filter.FileSources.IsNullOrEmpty())
                {
                    matched = matched & filter.FileSources.Contains(metaData.FileSource);
                }

                if (!filter.ContentTypes.IsNullOrEmpty())
                {
                    matched = matched && filter.ContentTypes.Contains(metaData.ContentType);
                }
            }
            

            if (!matched) continue;

            records.Add(metaData);
        }
        
        return new PagedItems<KnowledgeDocMetaData>
        {
            Items = records.Skip(filter.Offset).Take(filter.Size),
            Count = records.Count
        };
    }
    #endregion


    #region Private methods
    private string BuildKnowledgeCollectionConfigDir()
    {
        return Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, VECTOR_FOLDER);
    }

    private string BuildKnowledgeCollectionFileDir(string collectionName, string vectorStoreProvider)
    {
        return Path.Combine(_dbSettings.FileRepository, KNOWLEDGE_FOLDER, KNOWLEDGE_DOC_FOLDER, vectorStoreProvider.CleanStr(), collectionName.CleanStr());
    }
    #endregion
}
