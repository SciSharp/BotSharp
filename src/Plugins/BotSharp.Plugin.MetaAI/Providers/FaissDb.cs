using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.VectorStorage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MetaAI.Providers;

public class FaissDb : IVectorDb
{
    public Task CreateCollection(string collectionName, int dim)
    {
        throw new NotImplementedException();
    }

    public Task<KnowledgeCollectionInfo> GetCollectionInfo(string collectionName)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetCollections()
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> Search(string collectionName, float[] vector, string returnFieldName, int limit = 10, float confidence = 0.5f)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null)
    {
        throw new NotImplementedException();
    }
}
