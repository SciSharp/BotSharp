using BotSharp.Abstraction.VectorStorage;
using Microsoft.SemanticKernel.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel
{
    internal class SemanticKernelMemoryStoreProvider : IVectorDb
    {
        private readonly IMemoryStore _memoryStore;

        public SemanticKernelMemoryStoreProvider(IMemoryStore memoryStore)
        {
            this._memoryStore = memoryStore;
        }
        public async Task CreateCollection(string collectionName, int dim)
        {
            await _memoryStore.CreateCollectionAsync(collectionName);
        }

        public async Task<List<string>> GetCollections()
        {
            var result = new List<string>();
            await foreach (var collection in _memoryStore.GetCollectionsAsync())
            {
                result.Add(collection);
            }
            return result;
        }

        public async Task<List<string>> Search(string collectionName, float[] vector, int limit = 5)
        {
            var results = _memoryStore.GetNearestMatchesAsync(collectionName, vector, limit);

            var resultTexts = new List<string>();
            await foreach (var (record, _) in results)
            {
                resultTexts.Add(record.Metadata.Text);
            }

            return resultTexts;

        }

        public async Task Upsert(string collectionName, int id, float[] vector, string text)
        {
            await _memoryStore.UpsertAsync(collectionName, MemoryRecord.LocalRecord(id.ToString(), text, null, vector));
        }
    }
}
