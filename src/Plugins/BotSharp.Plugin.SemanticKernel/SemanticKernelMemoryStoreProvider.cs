using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage;
using BotSharp.Abstraction.VectorStorage.Enums;
using BotSharp.Abstraction.VectorStorage.Models;
using BotSharp.Abstraction.VectorStorage.Options;
using Microsoft.SemanticKernel.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
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


        public string Provider => "SemanticKernel";


        public async Task<bool> DoesCollectionExist(string collectionName)
        {
            return false;
        }

        public async Task<bool> CreateCollection(string collectionName, VectorCollectionCreateOptions options)
        {
            await _memoryStore.CreateCollectionAsync(collectionName);
            return true;
        }

        public async Task<bool> DeleteCollection(string collectionName)
        {
            await _memoryStore.DeleteCollectionAsync(collectionName);
            return false;
        }

        public Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids, VectorQueryOptions? options = null)
        {
            throw new NotImplementedException();
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

        public async Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector, VectorSearchOptions? options = null)
        {
            options ??= VectorSearchOptions.Default();
            var results = _memoryStore.GetNearestMatchesAsync(collectionName, vector, options.Limit.GetValueOrDefault());

            var resultTexts = new List<VectorCollectionData>();
            await foreach (var (record, score) in results)
            {
                resultTexts.Add(new VectorCollectionData
                {
                    Payload = new Dictionary<string, VectorPayloadValue>
                    {
                        { "text", new(record.Metadata.Text, VectorPayloadDataType.String) }
                    },
                    Score = score,
                    Vector = options.WithVector ? record.Embedding.ToArray() : null
                });
            }

            return resultTexts;
        }

        public async Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, VectorPayloadValue>? payload)
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            await _memoryStore.UpsertAsync(collectionName, MemoryRecord.LocalRecord(id.ToString(), text, null, vector));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return true;
        }

        public async Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids)
        {
            if (ids.IsNullOrEmpty()) return false;

            var exist = await _memoryStore.DoesCollectionExistAsync(collectionName);
            if (!exist) return false;

            await _memoryStore.RemoveBatchAsync(collectionName, ids.Select(x => x.ToString()));
            return true;
        }

        public async Task<bool> DeleteCollectionAllData(string collectionName)
        {
            return await Task.FromResult(false);
        }
    }
}
