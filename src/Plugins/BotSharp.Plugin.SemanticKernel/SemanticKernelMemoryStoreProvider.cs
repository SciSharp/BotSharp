using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.SemanticKernel.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel
{
    internal class SemanticKernelMemoryStoreProvider : IVectorDb
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly IMemoryStore _memoryStore;
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public SemanticKernelMemoryStoreProvider(IMemoryStore memoryStore)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            this._memoryStore = memoryStore;
        }
        public async Task CreateCollection(string collectionName, int dim)
        {
            await _memoryStore.CreateCollectionAsync(collectionName);
        }

        public Task<StringIdPagedItems<KnowledgeCollectionData>> GetCollectionData(string collectionName, KnowledgeFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public Task<KnowledgeCollectionInfo> GetCollectionInfo(string collectionName)
        {
            throw new System.NotImplementedException();
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

        public async Task<List<string>> Search(string collectionName, float[] vector, string returnFieldName, int limit = 5, float confidence = 0.5f)
        {
            var results = _memoryStore.GetNearestMatchesAsync(collectionName, vector, limit);

            var resultTexts = new List<string>();
            await foreach (var (record, _) in results)
            {
                resultTexts.Add(record.Metadata.Text);
            }

            return resultTexts;

        }

        public async Task<bool> Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload)
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            await _memoryStore.UpsertAsync(collectionName, MemoryRecord.LocalRecord(id.ToString(), text, null, vector));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return true;
        }
    }
}
