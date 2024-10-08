using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.VectorStorage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
{
    public List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds)
    {
        throw new NotImplementedException();
    }

    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
    {
        throw new NotImplementedException();
    }

    public bool DeleteKnowledgeCollectionConfig(string collectionName)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<VectorCollectionConfig> GetKnowledgeCollectionConfigs(VectorCollectionConfigFilter filter)
    {
        throw new NotImplementedException();
    }

    public bool SaveKnolwedgeBaseFileMeta(KnowledgeDocMetaData metaData)
    {
        throw new NotImplementedException();
    }

    public bool DeleteKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, Guid? fileId = null)
    {
        throw new NotImplementedException();
    }

    public PagedItems<KnowledgeDocMetaData> GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, KnowledgeFileFilter filter)
    {
        throw new NotImplementedException();
    }
}
