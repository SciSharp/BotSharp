using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage.Models;
using BotSharp.Plugin.Qdrant.Models;
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

    public async Task<bool> CreateCollection(string collectionName, int dimension)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (exist) return false;

        try
        {
            // Create a new collection
            var client = GetClient();
            await client.CreateCollectionAsync(collectionName, new VectorParams()
            {
                Size = (ulong)dimension,
                Distance = Distance.Cosine
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when create collection (Name: {collectionName}, Dimension: {dimension}).");
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
            PointsCount = details.PointsCount
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
        var tasks = new List<Task>();

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
            Data = x.Payload.ToDictionary(p => p.Key, p => p.Value.KindCase switch
            {
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
                Value.KindOneofCase.DoubleValue => p.Value.DoubleValue,
                _ => new object()
            }),
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
            Data = x.Payload?.ToDictionary(p => p.Key, p => p.Value.KindCase switch 
            { 
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
                Value.KindOneofCase.DoubleValue => p.Value.DoubleValue,
                _ => new object()
            }) ?? new(),
            Vector = x.Vectors?.Vector?.Data?.ToArray()
        });
    }

    public async Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, object>? payload = null)
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
                var value = item.Value?.ToString();
                if (value == null || item.Key.IsEqualTo(KnowledgePayloadName.Text))
                {
                    continue;
                }

                if (bool.TryParse(value, out var b))
                {
                    point.Payload[item.Key] = b;
                }
                else if (byte.TryParse(value, out var int8))
                {
                    point.Payload[item.Key] = int8;
                }
                else if (short.TryParse(value, out var int16))
                {
                    point.Payload[item.Key] = int16;
                }
                else if (int.TryParse(value, out var int32))
                {
                    point.Payload[item.Key] = int32;
                }
                else if (long.TryParse(value, out var int64))
                {
                    point.Payload[item.Key] = int64;
                }
                else if (float.TryParse(value, out var f32))
                {
                    point.Payload[item.Key] = f32;
                }
                else if (double.TryParse(value, out var f64))
                {
                    point.Payload[item.Key] = f64;
                }
                else if (DateTime.TryParse(value, out var dt))
                {
                    point.Payload[item.Key] = dt.ToUniversalTime().ToString("o");
                }
                else
                {
                    point.Payload[item.Key] = value;
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
            Data = x.Payload.ToDictionary(p => p.Key, p => p.Value.KindCase switch
            {
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
                Value.KindOneofCase.DoubleValue => p.Value.DoubleValue,
                _ => new object()
            }),
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
        var result = await client.CreatePayloadIndexAsync(collectionName, options.FieldName, schemaType, indexParams: new()
        {
            IntegerIndexParams = new()
            {
                Range = true
            }
        });
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
    private Filter? BuildQueryFilter(IEnumerable<VectorFilterGroup>? filterGroups)
    {
        Filter? queryFilter = null;

        if (filterGroups.IsNullOrEmpty())
        {
            return queryFilter;
        }

        var conditions = filterGroups.Where(x => !x.Filters.IsNullOrEmpty()).Select(x =>
        {
            Filter filter;
            var innerConditions = x.Filters.Select(f =>
            {
                var field = new FieldCondition
                {
                    Key = f.Key,
                    Match = new Match { Text = f.Value },
                };

                if (bool.TryParse(f.Value, out var boolVal))
                {
                    field.Match = new Match { Boolean = boolVal };
                }
                else if (long.TryParse(f.Value, out var intVal))
                {
                    field.Match = new Match { Integer = intVal };
                }

                return new Condition { Field = field };
            });

            if (x.FilterOperator.IsEqualTo("and"))
            {
                filter = new Filter
                {
                    Must = { innerConditions }
                };
            }
            else
            {
                filter = new Filter
                {
                    Should = { innerConditions }
                };
            }

            return new Condition
            {
                Filter = filter
            };
        });

        queryFilter = new Filter
        {
            Must =
            {
                conditions
            }
        };

        return queryFilter;
    }

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
            Direction = sort.Order == "asc" ? Direction.Asc : Direction.Desc
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
    #endregion
}
