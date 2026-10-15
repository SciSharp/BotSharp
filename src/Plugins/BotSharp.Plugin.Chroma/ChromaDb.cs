using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage.Options;
using ChromaDB.Client;
using ChromaDB.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace BotSharp.Plugin.Chroma;

public class ChromaDb : IVectorDb
{
    private ChromaClient? _client;
    private readonly ChromaSetting _setting;
    private readonly IServiceProvider _services;
    private readonly ILogger<ChromaDb> _logger;

    public ChromaDb(
        ChromaSetting setting,
        ILogger<ChromaDb> logger,
        IServiceProvider services)
    {
        _setting = setting;
        _logger = logger;
        _services = services;
    }

    public string Provider => "Chroma";

    private async Task<ChromaClient> GetClient()
    {
        if (_client == null)
        {
            var options = new ChromaConfigurationOptions
            (
                uri: _setting.Url,
                tenant: _setting.Tenant.IfNullOrEmptyAs(null),
                database: _setting.Database.IfNullOrEmptyAs(null),
                chromaToken: _setting.ApiKey.IfNullOrEmptyAs(null)
            );
            var http = _services.GetRequiredService<IHttpClientFactory>();
            var client = new ChromaClient(options, http.CreateClient());

            // On Chroma Cloud the API key gives the tenant and the database, as the CloudClient of the Python client
            _client = options.IsChromaCloud ? await client.WithTenantAndDatabaseFromIdentityAsync() : client;
        }
        return _client;
    }

    #region Collection
    public async Task<bool> DoesCollectionExist(string collectionName)
    {
        var client = await GetClient();
        return await client.CollectionExistsAsync(collectionName);
    }

    public async Task<bool> CreateCollection(string collectionName, VectorCollectionCreateOptions options)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (exist) return false;

        try
        {
            // Create a new collection: Chroma takes the dimension from the first vector it stores
            var client = await GetClient();
            await client.CreateCollectionAsync(new ChromaCollectionDefinition(collectionName)
            {
                Configuration = new() { Space = ChromaSpace.Cosine }
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when create collection (Name: {collectionName}, Dimension: {options.Dimension}).");
            return false;
        }
    }

    public async Task<bool> DeleteCollection(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (!exist) return false;

        var client = await GetClient();
        await client.DeleteCollectionAsync(collectionName);
        return true;
    }

    public async Task<IEnumerable<string>> GetCollections()
    {
        // List all the collections
        var client = await GetClient();
        var collections = await client.ListCollectionsAsync();
        return collections.Select(x => x.Name).ToList();
    }

    public async Task<VectorCollectionDetails?> GetCollectionDetails(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (!exist)
        {
            return null;
        }

        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);
        var count = (ulong)await collection.CountAsync();
        var indexedCount = (ulong)await collection.CountAsync(ChromaReadLevel.IndexOnly);

        return new VectorCollectionDetails
        {
            VectorsCount = count,
            IndexedVectorsCount = indexedCount,
            PointsCount = count
        };
    }
    #endregion

    #region Collection data
    public async Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return new StringIdPagedItems<VectorCollectionData>();
        }

        // Chroma pages by offset: the next id is the offset of the next page
        var where = BuildQueryFilter(filter.FilterGroups);
        var offset = int.TryParse(filter.StartId, out var start) ? start : 0;
        var include = ChromaGetInclude.Metadatas | ChromaGetInclude.Documents;
        if (filter.WithVector)
        {
            include |= ChromaGetInclude.Embeddings;
        }

        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);
        var totalCount = where == null
            ? (ulong)await collection.CountAsync()
            : (ulong)(await collection.GetAsync(where: where, include: ChromaGetInclude.None)).Count;
        var entries = await collection.GetAsync(where: where, limit: filter.Size, offset: offset, include: include);

        var points = entries.Select(x => new VectorCollectionData
        {
            Id = x.Id,
            Payload = MapPayload(x.Metadata, x.Document, filter.Fields),
            Vector = filter.WithVector ? x.Embedding?.ToArray() : null
        }).ToList();

        var nextOffset = offset + points.Count;
        return new StringIdPagedItems<VectorCollectionData>
        {
            Count = totalCount,
            NextId = points.Count == filter.Size && (ulong)nextOffset < totalCount ? nextOffset.ToString() : null,
            Items = points
        };
    }

    public async Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids, VectorQueryOptions? options = null)
    {
        if (ids.IsNullOrEmpty())
        {
            return Enumerable.Empty<VectorCollectionData>();
        }

        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return Enumerable.Empty<VectorCollectionData>();
        }

        var include = ChromaGetInclude.None;
        if (options?.WithPayload == true)
        {
            include |= ChromaGetInclude.Metadatas | ChromaGetInclude.Documents;
        }
        if (options?.WithVector == true)
        {
            include |= ChromaGetInclude.Embeddings;
        }

        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);
        var pointIds = ids.Select(x => x.ToString()).Distinct().ToList();
        var entries = await collection.GetAsync(pointIds, include: include);
        return entries.Select(x => new VectorCollectionData
        {
            Id = x.Id,
            Payload = MapPayload(x.Metadata, x.Document),
            Vector = x.Embedding?.ToArray()
        });
    }

    public async Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, VectorPayloadValue>? payload = null)
    {
        // The text is the document of the record, the other payload values its metadata
        var metadata = new Dictionary<string, object>();

        if (payload != null)
        {
            foreach (var item in payload)
            {
                if (item.Value == null || item.Key.IsEqualTo(KnowledgePayloadName.Text))
                {
                    continue;
                }

                var value = item.Value.DataValue?.ConvertToString();
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                switch (item.Value.DataType)
                {
                    case VectorPayloadDataType.Boolean when bool.TryParse(value, out var boolVal):
                        metadata[item.Key] = boolVal;
                        break;
                    case VectorPayloadDataType.Integer when long.TryParse(value, out var longVal):
                        metadata[item.Key] = longVal;
                        break;
                    case VectorPayloadDataType.Double when double.TryParse(value, out var doubleVal):
                        metadata[item.Key] = doubleVal;
                        break;
                    case VectorPayloadDataType.Datetime when DateTime.TryParse(value, out var dt):
                        metadata[item.Key] = dt.ToString("o");
                        break;
                    case VectorPayloadDataType.String:
                        metadata[item.Key] = value;
                        break;
                    default:
                        break;
                }
            }
        }

        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);

        // The payload replaces the stored one, as in Qdrant: Chroma keeps the keys an upsert does not send, so a null deletes each of them
        var stored = await collection.GetAsync(id.ToString(), include: ChromaGetInclude.Metadatas);
        foreach (var key in stored?.Metadata?.Keys ?? Enumerable.Empty<string>())
        {
            if (!metadata.ContainsKey(key))
            {
                metadata[key] = null!;
            }
        }

        await collection.UpsertAsync(new ChromaRecords([id.ToString()])
        {
            Embeddings = [vector],
            Metadatas = metadata.Count > 0 ? [metadata] : null,
            Documents = [text]
        });

        return true;
    }

    public async Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector, VectorSearchOptions? options = null)
    {
        var results = new List<VectorCollectionData>();

        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return results;
        }

        options ??= VectorSearchOptions.Default();
        var where = BuildQueryFilter(options.FilterGroups);
        var include = ChromaQueryInclude.Metadatas | ChromaQueryInclude.Documents | ChromaQueryInclude.Distances;
        if (options.WithVector)
        {
            include |= ChromaQueryInclude.Embeddings;
        }

        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);
        var entries = await collection.QueryAsync(new ChromaQuery([vector])
        {
            NResults = options.Limit.GetValueOrDefault(),
            Where = where,
            Include = include
        });

        // The collections have the cosine space: the score is the cosine similarity, 1 - the distance
        results = entries.Single()
            .Select(x => new VectorCollectionData
            {
                Id = x.Id,
                Payload = MapPayload(x.Metadata, x.Document, options.Fields),
                Score = 1 - x.Distance,
                Vector = options.WithVector ? x.Embedding?.ToArray() : null
            })
            .Where(x => options.Confidence == null || x.Score >= options.Confidence)
            .ToList();

        return results;
    }

    public async Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids)
    {
        if (ids.IsNullOrEmpty()) return false;

        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);
        await collection.DeleteAsync(ids.Select(x => x.ToString()).ToList());
        return true;
    }

    public async Task<bool> DeleteCollectionAllData(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        // Chroma deletes by ids or by filters: read the ids, then delete them
        var client = await GetClient();
        var collection = client.GetCollectionClient(collectionName);
        var ids = (await collection.GetAsync(include: ChromaGetInclude.None)).Select(x => x.Id).ToList();
        if (ids.Any())
        {
            await collection.DeleteAsync(ids);
        }
        return true;
    }
    #endregion

    #region Payload index
    public async Task<bool> CreateCollectionPayloadIndex(string collectionName, CollectionIndexOptions options)
    {
        // Chroma indexes every metadata key of a collection by default
        return await DoesCollectionExist(collectionName);
    }

    public async Task<bool> DeleteCollectionPayloadIndex(string collectionName, CollectionIndexOptions options)
    {
        // Chroma does not turn off the index of a key once the collection exists
        return await Task.FromResult(false);
    }
    #endregion

    #region Snapshots
    // Chroma has no snapshots of a collection
    public async Task<IEnumerable<VectorCollectionSnapshot>> GetCollectionSnapshots(string collectionName)
    {
        return await Task.FromResult(Enumerable.Empty<VectorCollectionSnapshot>());
    }

    public async Task<VectorCollectionSnapshot?> CreateCollectionShapshot(string collectionName)
    {
        return await Task.FromResult<VectorCollectionSnapshot?>(null);
    }

    public async Task<BinaryData> DownloadCollectionSnapshot(string collectionName, string snapshotFileName)
    {
        return await Task.FromResult(BinaryData.Empty);
    }

    public async Task<bool> RecoverCollectionFromShapshot(string collectionName, string snapshotFileName, BinaryData snapshotData)
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> DeleteCollectionShapshot(string collectionName, string snapshotName)
    {
        return await Task.FromResult(false);
    }
    #endregion


    #region Private methods
    #region Query filter
    private ChromaWhereOperator? BuildQueryFilter(IEnumerable<VectorFilterGroup>? filterGroups)
    {
        if (filterGroups.IsNullOrEmpty())
        {
            return null;
        }

        var groupConditions = filterGroups!.Select(x => BuildFilterGroupCondition(x))
                                          .Where(c => c != null)
                                          .ToList();

        if (groupConditions.IsNullOrEmpty())
        {
            return null;
        }

        // Multiple groups are combined with AND, as in the other vector databases
        return groupConditions.Aggregate((a, b) => a! & b!);
    }

    private ChromaWhereOperator? BuildFilterGroupCondition(VectorFilterGroup group)
    {
        if (group.Filters.IsNullOrEmpty())
        {
            return null;
        }

        var subGroupConditions = group.Filters!
                                      .Select(x => BuildSubGroupCondition(x))
                                      .Where(c => c != null)
                                      .ToList();

        return Combine(subGroupConditions, group.LogicalOperator);
    }

    private ChromaWhereOperator? BuildSubGroupCondition(VectorFilterSubGroup subGroup)
    {
        if (subGroup.Operands.IsNullOrEmpty())
        {
            return null;
        }

        var operandConditions = subGroup.Operands
                                        .Select(x => BuildOperandCondition(x))
                                        .Where(c => c != null)
                                        .ToList();

        return Combine(operandConditions, subGroup.LogicalOperator);
    }

    private ChromaWhereOperator? Combine(List<ChromaWhereOperator?> conditions, string logicalOperator)
    {
        if (conditions.IsNullOrEmpty())
        {
            return null;
        }

        return logicalOperator.IsEqualTo("and")
            ? conditions.Aggregate((a, b) => a! & b!)
            : conditions.Aggregate((a, b) => a! | b!);
    }

    private ChromaWhereOperator? BuildOperandCondition(VectorFilterOperand operand)
    {
        ChromaWhereOperator? condition = null;

        if (operand.Match != null)
        {
            condition = BuildMatchCondition(operand.Match);
        }
        else if (operand.Range != null)
        {
            condition = BuildRangeCondition(operand.Range);
        }

        return condition;
    }

    private ChromaWhereOperator? BuildMatchCondition(VectorFilterMatch match)
    {
        if (string.IsNullOrEmpty(match.Key) || match.Value == null)
        {
            return null;
        }

        object value = match.Value;
        if (match.DataType == VectorPayloadDataType.Boolean
            && bool.TryParse(match.Value, out var boolVal))
        {
            value = boolVal;
        }
        else if (match.DataType == VectorPayloadDataType.Integer
            && long.TryParse(match.Value, out var longVal))
        {
            value = longVal;
        }
        else if (match.DataType == VectorPayloadDataType.Double
            && double.TryParse(match.Value, out var doubleVal))
        {
            value = doubleVal;
        }

        return match.Operator.IsEqualTo("eq")
            ? ChromaWhereOperator.Equal(match.Key, value)
            : ChromaWhereOperator.NotEqual(match.Key, value);
    }

    private ChromaWhereOperator? BuildRangeCondition(VectorFilterRange range)
    {
        if (string.IsNullOrEmpty(range.Key) || range.Conditions.IsNullOrEmpty())
        {
            return null;
        }

        // Chroma compares numbers only: a datetime is stored as text
        if (range.DataType != VectorPayloadDataType.Integer
            && range.DataType != VectorPayloadDataType.Double)
        {
            _logger.LogWarning($"Chroma does not filter {range.DataType} values by range (Key: {range.Key}).");
            return null;
        }

        var conditions = new List<ChromaWhereOperator?>();
        foreach (var condition in range.Conditions)
        {
            // An integer value stays an integer: Chroma Cloud does not compare a float with integer values
            object value;
            if (range.DataType == VectorPayloadDataType.Integer
                && long.TryParse(condition.Value, out var longVal))
            {
                value = longVal;
            }
            else if (double.TryParse(condition.Value, out var doubleVal))
            {
                value = doubleVal;
            }
            else
            {
                continue;
            }

            switch (condition.Operator.ToLower())
            {
                case "lt":
                    conditions.Add(ChromaWhereOperator.LessThan(range.Key, value));
                    break;
                case "lte":
                    conditions.Add(ChromaWhereOperator.LessThanOrEqual(range.Key, value));
                    break;
                case "gt":
                    conditions.Add(ChromaWhereOperator.GreaterThan(range.Key, value));
                    break;
                case "gte":
                    conditions.Add(ChromaWhereOperator.GreaterThanOrEqual(range.Key, value));
                    break;
            }
        }

        return Combine(conditions, "and");
    }
    #endregion

    private Dictionary<string, VectorPayloadValue> MapPayload(IReadOnlyDictionary<string, object>? metadata, string? document, IEnumerable<string>? fields = null)
    {
        var payload = new Dictionary<string, VectorPayloadValue>();
        if (document != null)
        {
            payload[KnowledgePayloadName.Text] = VectorPayloadValue.BuildStringValue(document);
        }

        foreach (var item in metadata ?? new Dictionary<string, object>())
        {
            payload[item.Key] = item.Value switch
            {
                string s => VectorPayloadValue.BuildStringValue(s),
                bool b => VectorPayloadValue.BuildBooleanValue(b),
                long l => VectorPayloadValue.BuildIntegerValue(l),
                int i => VectorPayloadValue.BuildIntegerValue(i),
                double d => VectorPayloadValue.BuildDoubleValue(d),
                float f => VectorPayloadValue.BuildDoubleValue(f),
                _ => VectorPayloadValue.BuildUnkownValue(string.Empty)
            };
        }

        if (fields.IsNullOrEmpty())
        {
            return payload;
        }

        // Only the payload fields asked for, as the other vector databases return
        return payload.Where(x => fields!.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }
    #endregion
}
