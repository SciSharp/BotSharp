using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage;
using BotSharp.Abstraction.VectorStorage.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MetaAI.Providers;

public class FaissDb : IVectorDb
{
    public string Provider => "Faiss";

    public Task<bool> CreateCollection(string collectionName, int dimension)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteCollection(string collectionName)
    {
        throw new NotImplementedException();
    }

    public Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids,
        bool withPayload = false, bool withVector = false)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetCollections()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector,
        IEnumerable<string>? fields, int limit = 10, float confidence = 0.5f, bool withVector = false)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, string>? payload = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids)
    {
        throw new NotImplementedException();
    }
}
