using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage;
using BotSharp.Abstraction.VectorStorage.Models;
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


        public string Name => "SemanticKernel";

        public async Task CreateCollection(string collectionName, int dim)
        {
            await _memoryStore.CreateCollectionAsync(collectionName);
        }

        public Task<StringIdPagedItems<VectorCollectionData>> GetCollectionData(string collectionName, VectorFilter filter)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetCollections()
        {
            var result = new List<string>();
            await foreach (var collection in _memoryStore.GetCollectionsAsync())
            {
                result.Add(collection);
            }
            return result;
        }

        public async Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector,
            IEnumerable<string>? fields, int limit = 5, float confidence = 0.5f, bool withVector = false)
        {
            var results = _memoryStore.GetNearestMatchesAsync(collectionName, vector, limit);

            var resultTexts = new List<VectorCollectionData>();
            await foreach (var (record, score) in results)
            {
                resultTexts.Add(new VectorCollectionData
                {
                    Data = new Dictionary<string, string> { { "text", record.Metadata.Text } },
                    Score = score,
                    Vector = withVector ? record.Embedding.ToArray() : null
                });
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

        public async Task<bool> DeleteCollectionData(string collectionName, string id)
        {
            var exist = await _memoryStore.DoesCollectionExistAsync(collectionName);

            if (exist)
            {
                await _memoryStore.RemoveAsync(collectionName, id);
                return true;
            }
            return false;
        }
    }
}
