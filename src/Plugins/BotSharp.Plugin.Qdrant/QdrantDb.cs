using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage.Models;
using BotSharp.Plugin.Qdrant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
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
    #endregion

    #region Collection data
    public async Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return new StringIdPagedItems<VectorCollectionData>();
        }

        // Build query filter
        Filter? queryFilter = null;
        if (!filter.SearchPairs.IsNullOrEmpty())
        {
            var conditions = filter.SearchPairs.Select(x => new Condition
            {
                Field = new FieldCondition
                {
                    Key = x.Key,
                    Match = new Match { Text = x.Value },
                }
            });

            queryFilter = new Filter
            {
                Should =
                {
                    conditions
                }
            };
        }

        // Build payload selector
        WithPayloadSelector? payloadSelector = null;
        if (!filter.IncludedPayloads.IsNullOrEmpty())
        {
            payloadSelector = new WithPayloadSelector
            { 
                Enable = true,
                Include = new PayloadIncludeSelector
                {
                    Fields = { filter.IncludedPayloads.ToArray() }
                }
            };
        }

        var client = GetClient();
        var totalPointCount = await client.CountAsync(collectionName, filter: queryFilter);
        var response = await client.ScrollAsync(collectionName, limit: (uint)filter.Size, 
            offset: !string.IsNullOrWhiteSpace(filter.StartId) ? new PointId { Uuid = filter.StartId } : null,
            filter: queryFilter,
            payloadSelector: payloadSelector,
            vectorsSelector: filter.WithVector);

        var points = response?.Result?.Select(x => new VectorCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Data = x.Payload.ToDictionary(p => p.Key, p => p.Value.KindCase switch
            {
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
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


    public async Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids,
        bool withPayload = false, bool withVector = false)
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
        var points = await client.RetrieveAsync(collectionName, pointIds, withPayload, withVector);
        return points.Select(x => new VectorCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Data = x.Payload?.ToDictionary(p => p.Key, p => p.Value.KindCase switch 
            { 
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
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
                if (value == null) continue;

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

    public async Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector,
        IEnumerable<string>? fields, int limit = 5, float confidence = 0.5f, bool withVector = false)
    {
        var results = new List<VectorCollectionData>();

        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return results;
        }

        var payloadSelector = new WithPayloadSelector { Enable = true };
        if (fields != null)
        {
            payloadSelector.Include = new PayloadIncludeSelector { Fields = { fields.ToArray() } };
        }

        var client = GetClient();
        var points = await client.SearchAsync(collectionName,
                                            vector,
                                            limit: (ulong)limit,
                                            scoreThreshold: confidence,
                                            payloadSelector: payloadSelector,
                                            vectorsSelector: withVector);

        results = points.Select(x => new VectorCollectionData
        {
            Id = x.Id.Uuid,
            Data = x.Payload.ToDictionary(p => p.Key, p => p.Value.KindCase switch
            {
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
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
                _logger.LogError($"Error when downloading Qdrant snapshot (Endpoint: {url}, Snapshot: {snapshotFileName}). {ex.Message}\r\n{ex.InnerException}");
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
                _logger.LogError($"Error when uploading Qdrant snapshot (Endpoint: {url}, Snapshot: {snapshotFileName}). {ex.Message}\r\n{ex.InnerException}");
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
}
