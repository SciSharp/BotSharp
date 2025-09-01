using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Utilities;
using BotSharp.Plugin.Qdrant.Models;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Collections;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;

namespace BotSharp.Plugin.Qdrant;

public class QdrantDb : IVectorDb
{
    private QdrantClient _client;
    private readonly QdrantSetting _setting;
    private readonly BotSharpOptions _options;
    private readonly IServiceProvider _services;
    private readonly ILogger<QdrantDb> _logger;

    public QdrantDb(
        QdrantSetting setting,
        BotSharpOptions options,
        ILogger<QdrantDb> logger,
        IServiceProvider services)
    {
        _setting = setting;
        _options = options;
        _logger = logger;
        _services = services;
    }

    public string Provider => "Qdrant";

    private QdrantClient GetClient()
    {
        if (_client == null)
        {
            _client = new QdrantClient
            (
                host: _setting.Url,
                https: true,
                apiKey: _setting.ApiKey
            );
        }
        return _client;
    }

    #region Collection
    public async Task<bool> DoesCollectionExist(string collectionName)
    {
        var client = GetClient();
        return await client.CollectionExistsAsync(collectionName);
    }

    public async Task<bool> CreateCollection(string collectionName, VectorCollectionCreateOptions options)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (exist) return false;

        try
        {
            // Create a new collection
            var client = GetClient();
            await client.CreateCollectionAsync(collectionName, new VectorParams()
            {
                Size = (ulong)options.Dimension,
                Distance = Distance.Cosine
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when create collection (Name: {collectionName}, Dimension: {options.Dimension}).");
            return false;
        }
    }

    public async Task<bool> DeleteCollection(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (!exist) return false;

        var client = GetClient();
        await client.DeleteCollectionAsync(collectionName);
        return true;
    }

    public async Task<IEnumerable<string>> GetCollections()
    {
        // List all the collections
        var collections = await GetClient().ListCollectionsAsync();
        return collections.ToList();
    }

    public async Task<VectorCollectionDetails?> GetCollectionDetails(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (!exist) return null;

        var client = GetClient();
        var details = await client.GetCollectionInfoAsync(collectionName);

        if (details == null) return null;

        var payloadSchema = details.PayloadSchema?.Select(x => new PayloadSchemaDetail
        {
            FieldName = x.Key,
            FieldDataType = x.Value.DataType.ToString().ToLowerInvariant(),
            DataCount = x.Value.Points
        })?.ToList() ?? [];

        return new VectorCollectionDetails
        {
            Status = details.Status.ToString(),
            OptimizerStatus = details.OptimizerStatus.ToString(),
            SegmentsCount = details.SegmentsCount,
            InnerConfig = new VectorCollectionDetailConfig
            {
                Param = new VectorCollectionDetailConfigParam
                {
                    ShardNumber = details.Config?.Params?.ShardNumber,
                    ShardingMethod = details.Config?.Params?.ShardingMethod.ToString(),
                    ReplicationFactor = details.Config?.Params?.ReplicationFactor,
                    WriteConsistencyFactor = details.Config?.Params?.WriteConsistencyFactor,
                    ReadFanOutFactor = details.Config?.Params?.ReadFanOutFactor
                }
            },
            VectorsCount = details.VectorsCount,
            IndexedVectorsCount = details.IndexedVectorsCount,
            PointsCount = details.PointsCount,
            PayloadSchema = payloadSchema
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

        Filter? queryFilter = BuildQueryFilter(filter.FilterGroups);
        WithPayloadSelector? payloadSelector = BuildPayloadSelector(filter.Fields);
        OrderBy? orderBy = BuildOrderBy(filter.OrderBy);

        var client = GetClient();

        var totalCountTask = client.CountAsync(collectionName, filter: queryFilter);
        var dataResponseTask = client.ScrollAsync(
            collectionName,
            limit: (uint)filter.Size,
            offset: !string.IsNullOrWhiteSpace(filter.StartId) ? new PointId { Uuid = filter.StartId } : null,
            filter: queryFilter,
            orderBy: orderBy,
            payloadSelector: payloadSelector,
            vectorsSelector: filter.WithVector);

        await Task.WhenAll([totalCountTask, dataResponseTask]);

        var totalPointCount = totalCountTask.Result;
        var response = dataResponseTask.Result;

        var points = response?.Result?.Select(x => new VectorCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Payload = MapPayload(x.Payload),
            Vector = filter.WithVector ? x.Vectors?.Vector?.Data?.ToArray() : null
        })?.ToList() ?? new List<VectorCollectionData>();

        return new StringIdPagedItems<VectorCollectionData>
        {
            Count = totalPointCount,
            NextId = response?.NextPageOffset?.Uuid,
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

        var client = GetClient();
        var pointIds = ids.Select(x => new PointId { Uuid = x.ToString() }).Distinct().ToList();
        var points = await client.RetrieveAsync(collectionName, pointIds, options?.WithPayload ?? false, options?.WithVector ?? false);
        return points.Select(x => new VectorCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Payload = MapPayload(x.Payload),
            Vector = x.Vectors?.Vector?.Data?.ToArray()
        });
    }

    public async Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, VectorPayloadValue>? payload = null)
    {
        // Insert vectors
        var point = new PointStruct()
        {
            Id = new PointId()
            {
                Uuid = id.ToString()
            },
            Vectors = vector,
            Payload =
            {
                { KnowledgePayloadName.Text, text }
            }
        };

        if (payload != null)
        {
            foreach (var item in payload)
            {
                if (item.Value == null)
                {
                    continue;
                }

                var value = item.Value.DataValue?.ConvertToString();
                if (string.IsNullOrEmpty(value) || item.Key.IsEqualTo(KnowledgePayloadName.Text))
                {
                    continue;
                }

                switch (item.Value.DataType)
                {
                    case VectorPayloadDataType.Boolean when bool.TryParse(value, out var boolVal):
                        point.Payload[item.Key] = boolVal;
                        break;
                    case VectorPayloadDataType.Integer when long.TryParse(value, out var longVal):
                        point.Payload[item.Key] = longVal;
                        break;
                    case VectorPayloadDataType.Double when double.TryParse(value, out var doubleVal):
                        point.Payload[item.Key] = doubleVal;
                        break;
                    case VectorPayloadDataType.Datetime when DateTime.TryParse(value, out var dt):
                        point.Payload[item.Key] = dt.ToString("o");
                        break;
                    case VectorPayloadDataType.String:
                        point.Payload[item.Key] = value;
                        break;
                    default:
                        break;
                }
            }
        }

        var client = GetClient();
        var result = await client.UpsertAsync(collectionName, points: new List<PointStruct>
        {
            point
        });

        return result.Status == UpdateStatus.Completed;
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
        Filter? queryFilter = BuildQueryFilter(options.FilterGroups);
        WithPayloadSelector? payloadSelector = BuildPayloadSelector(options.Fields);

        var client = GetClient();
        var points = await client.SearchAsync(collectionName,
                                            vector,
                                            limit: (ulong)options.Limit.GetValueOrDefault(),
                                            scoreThreshold: options.Confidence,
                                            filter: queryFilter,
                                            payloadSelector: payloadSelector,
                                            vectorsSelector: options.WithVector);

        results = points.Select(x => new VectorCollectionData
        {
            Id = x.Id.Uuid,
            Payload = MapPayload(x.Payload),
            Score = x.Score,
            Vector = x.Vectors?.Vector?.Data?.ToArray()
        }).ToList();

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

        var client = GetClient();
        var result = await client.DeleteAsync(collectionName, ids);
        return result.Status == UpdateStatus.Completed;
    }

    public async Task<bool> DeleteCollectionAllData(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        var client = GetClient();
        var result = await client.DeleteAsync(collectionName, new Filter());
        return result.Status == UpdateStatus.Completed;
    }
    #endregion

    #region Payload index
    public async Task<bool> CreateCollectionPayloadIndex(string collectionName, CreateVectorCollectionIndexOptions options)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        var client = GetClient();
        var schemaType = ConvertPayloadSchemaType(options.FieldSchemaType);
        var result = await client.CreatePayloadIndexAsync(collectionName, options.FieldName, schemaType);
        return result.Status == UpdateStatus.Completed;
    }

    public async Task<bool> DeleteCollectionPayloadIndex(string collectionName, DeleteVectorCollectionIndexOptions options)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        var client = GetClient();
        var result = await client.DeletePayloadIndexAsync(collectionName, options.FieldName);
        return result.Status == UpdateStatus.Completed;
    }
    #endregion

    #region Snapshots
    public async Task<IEnumerable<VectorCollectionSnapshot>> GetCollectionSnapshots(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return Enumerable.Empty<VectorCollectionSnapshot>();
        }

        var client = GetClient();
        var data = await client.ListSnapshotsAsync(collectionName);
        var snapshots = data.Select(x => new VectorCollectionSnapshot
        {
            Name = x.Name,
            Size = x.Size,
            CreatedTime = x.CreationTime.ToDateTime(),
            CheckSum = x.Checksum
        });
        return snapshots;
    }

    public async Task<VectorCollectionSnapshot?> CreateCollectionShapshot(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return null;
        }

        var client = GetClient();
        var desc = await client.CreateSnapshotAsync(collectionName);
        if (desc == null)
        {
            return null;
        }

        return new VectorCollectionSnapshot
        {
            Name = desc.Name,
            Size = desc.Size,
            CreatedTime = desc.CreationTime.ToDateTime(),
            CheckSum = desc.Checksum
        };
    }

    public async Task<BinaryData> DownloadCollectionSnapshot(string collectionName, string snapshotFileName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return BinaryData.Empty;
        }

        var domain = $"https://{_setting.Url}:6333";
        var url = $"{domain}/collections/{collectionName}/snapshots/{snapshotFileName}";

        var http = _services.GetRequiredService<IHttpClientFactory>();
        using (var client = http.CreateClient())
        {
            try
            {
                var uri = new Uri(url);
                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = uri
                };

                client.DefaultRequestHeaders.Add("api-key", _setting.ApiKey);
                var rawResponse = await client.SendAsync(message);
                rawResponse.EnsureSuccessStatusCode();

                using var contentStream = await rawResponse.Content.ReadAsStreamAsync();
                return BinaryData.FromStream(contentStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when downloading Qdrant snapshot (Endpoint: {url}, Snapshot: {snapshotFileName}).");
                return BinaryData.Empty;
            }
        }
    }

    public async Task<bool> RecoverCollectionFromShapshot(string collectionName, string snapshotFileName, BinaryData snapshotData)
    {
        var domain = $"https://{_setting.Url}:6333";
        var url = $"{domain}/collections/{collectionName}/snapshots/upload";

        var http = _services.GetRequiredService<IHttpClientFactory>();
        using (var client = http.CreateClient())
        {
            try
            {
                var uri = new Uri(url);
                var data = new MultipartFormDataContent
                {
                    { new StringContent(snapshotFileName), "name" },
                    { new StringContent(MediaTypeNames.Application.Octet), "type" },
                    { new StreamContent(snapshotData.ToStream()), "snapshot", snapshotFileName }
                };

                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = data
                };

                client.DefaultRequestHeaders.Add("api-key", _setting.ApiKey);
                var rawResponse = await client.SendAsync(message);
                rawResponse.EnsureSuccessStatusCode();

                var responseStr = await rawResponse.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<RecoverFromSnapshotResponse>(responseStr, _options.JsonSerializerOptions);
                return response?.Result == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when uploading Qdrant snapshot (Endpoint: {url}, Snapshot: {snapshotFileName}).");
                return false;
            }
        }
    }

    public async Task<bool> DeleteCollectionShapshot(string collectionName, string snapshotName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        try
        {
            var client = GetClient();
            await client.DeleteSnapshotAsync(collectionName, snapshotName);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion


    #region Private methods
    #region Query filter
    private Filter? BuildQueryFilter(IEnumerable<VectorFilterGroup>? filterGroups)
    {
        if (filterGroups.IsNullOrEmpty())
        {
            return null;
        }

        var groupConditions = filterGroups.Select(x => BuildFilterGroupCondition(x))
                                          .Where(c => c != null)
                                          .ToList();

        if (groupConditions.IsNullOrEmpty())
        {
            return null;
        }

        // If there's only one group, return it directly to avoid unnecessary nesting
        if (groupConditions.Count == 1 && groupConditions[0].Filter != null)
        {
            return groupConditions[0].Filter;
        }

        // Multiple groups are combined with AND by default
        // This follows Qdrant's convention where multiple top-level conditions are ANDed
        return new Filter
        {
            Must = { groupConditions }
        };
    }

    private Condition? BuildFilterGroupCondition(VectorFilterGroup group)
    {
        if (group.Filters.IsNullOrEmpty())
        {
            return null;
        }

        var subGroupConditions = group.Filters
                                      .Select(x => BuildSubGroupCondition(x))
                                      .Where(c => c != null)
                                      .ToList();

        if (subGroupConditions.IsNullOrEmpty())
        {
            return null;
        }

        // If there's only one subgroup, return it directly
        if (subGroupConditions.Count == 1 && subGroupConditions[0].Filter != null)
        {
            return subGroupConditions[0];
        }

        // Apply the group operator to combine subgroups
        var filter = new Filter();
        if (group.LogicalOperator.IsEqualTo("and"))
        {
            filter = new Filter
            {
                Must = { subGroupConditions }
            };
        }
        else // "or"
        {
            filter = new Filter
            {
                Should = { subGroupConditions }
            };
        }

        return new Condition { Filter = filter };
    }

    private Condition? BuildSubGroupCondition(VectorFilterSubGroup subGroup)
    {
        if (subGroup.Operands.IsNullOrEmpty())
        {
            return null;
        }

        var operandConditions = subGroup.Operands
                                        .Select(x => BuildOperandCondition(x))
                                        .Where(c => c != null)
                                        .ToList();

        if (operandConditions.IsNullOrEmpty())
        {
            return null;
        }

        // If there's only one operand, return it directly
        if (operandConditions.Count == 1 && operandConditions[0].Filter != null)
        {
            return operandConditions[0];
        }

        // Apply the subgroup operator to combine operands
        var filter = new Filter();
        if (subGroup.LogicalOperator.IsEqualTo("and"))
        {
            filter = new Filter
            {
                Must = { operandConditions }
            };
        }
        else // "or"
        {
            filter = new Filter
            {
                Should = { operandConditions }
            };
        }

        return new Condition { Filter = filter };
    }

    private Condition? BuildOperandCondition(VectorFilterOperand operand)
    {
        Condition? condition = null;

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

    private Condition? BuildMatchCondition(VectorFilterMatch match)
    {
        var fieldCondition = BuildMatchFieldCondition(match);
        if (fieldCondition == null)
        {
            return null;
        }

        Condition condition;
        if (match.Operator.IsEqualTo("eq"))
        {
            var filter = new Filter()
            {
                Must = { new Condition { Field = fieldCondition } }
            };
            condition = new Condition { Filter = filter };
        }
        else
        {
            var filter = new Filter()
            {
                MustNot = { new Condition { Field = fieldCondition } }
            };
            condition = new Condition { Filter = filter };
        }

        return condition;
    }

    private FieldCondition? BuildMatchFieldCondition(VectorFilterMatch match)
    {
        if (string.IsNullOrEmpty(match.Key) || match.Value == null)
        {
            return null;
        }

        var fieldCondition = new FieldCondition { Key = match.Key };

        if (match.DataType == VectorPayloadDataType.Boolean
            && bool.TryParse(match.Value, out var boolVal))
        {
            fieldCondition.Match = new Match { Boolean = boolVal };
        }
        else if (match.DataType == VectorPayloadDataType.Integer
            && long.TryParse(match.Value, out var longVal))
        {
            fieldCondition.Match = new Match { Integer = longVal };
        }
        else
        {
            fieldCondition.Match = new Match { Text = match.Value };
        }

        return fieldCondition;
    }

    private Condition? BuildRangeCondition(VectorFilterRange range)
    {
        var fieldCondition = BuildRangeFieldCondition(range);
        if (fieldCondition == null)
        {
            return null;
        }

        return new Condition
        {
            Field = fieldCondition
        };
    }

    private FieldCondition? BuildRangeFieldCondition(VectorFilterRange range)
    {
        if (string.IsNullOrEmpty(range.Key) || range.Conditions.IsNullOrEmpty())
        {
            return null;
        }

        FieldCondition? fieldCondition = null;

        if (range.DataType == VectorPayloadDataType.Datetime)
        {
            fieldCondition = new FieldCondition { Key = range.Key, DatetimeRange = new() };

            foreach (var condition in range.Conditions)
            {
                if (!DateTime.TryParse(condition.Value, out var dt))
                {
                    continue;
                }

                var utc = dt.ToUniversalTime();
                var seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
                var nanos = (int)((utc.Ticks % TimeSpan.TicksPerSecond) * 100);
                var timestamp = new Google.Protobuf.WellKnownTypes.Timestamp { Seconds = seconds, Nanos = nanos };

                switch (condition.Operator.ToLower())
                {
                    case "lt":
                        fieldCondition.DatetimeRange.Lt = timestamp;
                        break;
                    case "lte":
                        fieldCondition.DatetimeRange.Lte = timestamp;
                        break;
                    case "gt":
                        fieldCondition.DatetimeRange.Gt = timestamp;
                        break;
                    case "gte":
                        fieldCondition.DatetimeRange.Gte = timestamp;
                        break;
                }
            }
        }
        else if (range.DataType == VectorPayloadDataType.Integer
            || range.DataType == VectorPayloadDataType.Double)
        {
            fieldCondition = new FieldCondition { Key = range.Key, Range = new() };

            foreach (var condition in range.Conditions)
            {
                if (!double.TryParse(condition.Value, out var doubleVal))
                {
                    continue;
                }

                switch (condition.Operator.ToLower())
                {
                    case "lt":
                        fieldCondition.Range.Lt = doubleVal;
                        break;
                    case "lte":
                        fieldCondition.Range.Lte = doubleVal;
                        break;
                    case "gt":
                        fieldCondition.Range.Gt = doubleVal;
                        break;
                    case "gte":
                        fieldCondition.Range.Gte = doubleVal;
                        break;
                }
            }
        }

        return fieldCondition;
    }
    #endregion

    private WithPayloadSelector? BuildPayloadSelector(IEnumerable<string>? payloads)
    {
        WithPayloadSelector? payloadSelector = null;
        if (!payloads.IsNullOrEmpty())
        {
            payloadSelector = new WithPayloadSelector
            {
                Enable = true,
                Include = new PayloadIncludeSelector
                {
                    Fields = { payloads.ToArray() }
                }
            };
        }

        return payloadSelector;
    }

    private OrderBy? BuildOrderBy(VectorSort? sort)
    {
        if (string.IsNullOrWhiteSpace(sort?.Field))
        {
            return null;
        }

        return new OrderBy
        {
            Key = sort.Field,
            Direction = sort.Order.IsEqualTo("asc") ? Direction.Asc : Direction.Desc
        };
    }

    private PayloadSchemaType ConvertPayloadSchemaType(string schemaType)
    {
        PayloadSchemaType res;
        switch (schemaType.ToLower())
        {
            case "text":
                res = PayloadSchemaType.Text;
                break;
            case "keyword":
                res = PayloadSchemaType.Keyword;
                break;
            case "integer":
                res = PayloadSchemaType.Integer;
                break;
            case "float":
                res = PayloadSchemaType.Float;
                break;
            case "bool":
            case "boolean":
                res = PayloadSchemaType.Bool;
                break;
            case "geo":
                res = PayloadSchemaType.Geo;
                break;
            case "datetime":
                res = PayloadSchemaType.Datetime;
                break;
            case "uuid":
                res = PayloadSchemaType.Uuid;
                break;
            default:
                res = PayloadSchemaType.UnknownType;
                break;
        }

        return res;
    }

    private Dictionary<string, VectorPayloadValue> MapPayload(MapField<string, Value>? payload)
    {
        return payload?.ToDictionary(p => p.Key, p => p.Value.KindCase switch
        {
            Value.KindOneofCase.StringValue => VectorPayloadValue.BuildStringValue(p.Value.StringValue),
            Value.KindOneofCase.BoolValue => VectorPayloadValue.BuildBooleanValue(p.Value.BoolValue),
            Value.KindOneofCase.IntegerValue => VectorPayloadValue.BuildIntegerValue(p.Value.IntegerValue),
            Value.KindOneofCase.DoubleValue => VectorPayloadValue.BuildDoubleValue(p.Value.DoubleValue),
            _ => VectorPayloadValue.BuildUnkownValue(string.Empty)
        }) ?? [];
    }
    #endregion
}
