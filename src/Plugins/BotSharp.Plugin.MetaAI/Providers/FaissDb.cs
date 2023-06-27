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

    public Task<List<string>> GetCollections()
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> Search(string collectionName, float[] vector, int limit = 10)
    {
        throw new NotImplementedException();
    }

    public Task Upsert(string collectionName, int id, float[] vector, string text)
    {
        throw new NotImplementedException();
    }
}
