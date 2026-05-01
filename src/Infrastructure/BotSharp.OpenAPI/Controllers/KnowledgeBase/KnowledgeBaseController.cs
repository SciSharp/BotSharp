using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Knowledges.Enums;
using BotSharp.Abstraction.Repositories;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public partial class KnowledgeBaseController : ControllerBase
{
    private readonly IGraphKnowledgeService _graphKnowledgeService;
    private readonly IServiceProvider _services;

    public KnowledgeBaseController(
        IGraphKnowledgeService graphKnowledgeService,
        IServiceProvider services)
    {
        _graphKnowledgeService = graphKnowledgeService;
        _services = services;
    }

    #region Collection
    [HttpGet("knowledge/collection/{collection}/exist")]
    public async Task<bool> ExistCollection([FromRoute] string collection, [FromQuery] string knowledgeType, [FromQuery] string? dbProvider = null)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (kg == null)
        {
            return false;
        }

        return await kg.ExistCollection(collection, new KnowledgeCollectionOptions { DbProvider = dbProvider });
    }

    [HttpGet("knowledge/collections")]
    public async Task<IEnumerable<KnowledgeCollectionConfigViewModel>> GetCollections([FromQuery] string? knowledgeType, [FromQuery] string? dbProvider = null)
    {
        var results = new List<KnowledgeCollectionConfigViewModel>();

        if (!string.IsNullOrWhiteSpace(knowledgeType))
        {
            var kg = _services.GetServices<IKnowledgeOrchestrator>()
                              .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

            if (kg == null)
            {
                return [];
            }

            var collections = await kg.GetCollections(new KnowledgeCollectionOptions { DbProvider = dbProvider });
            results = collections.Select(x => KnowledgeCollectionConfigViewModel.From(x)).ToList();
        }
        else
        {
            var kgs = _services.GetServices<IKnowledgeOrchestrator>();
            foreach (var kg in kgs)
            {
                var collections = await kg.GetCollections(new KnowledgeCollectionOptions { DbProvider = dbProvider });
                var res = collections.Select(x => KnowledgeCollectionConfigViewModel.From(x));
                results.AddRange(res);
            }
        }

        return results;
    }

    [HttpGet("knowledge/collection/{collection}/details")]
    public async Task<KnowledgeCollectionDetails?> GetCollectionDetails([FromRoute] string collection, [FromQuery] string knowledgeType, [FromQuery] string? dbProvider = null)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (kg == null)
        {
            return null;
        }

        return await kg.GetCollectionDetails(collection, new KnowledgeCollectionOptions { DbProvider = dbProvider });
    }

    [HttpPost("knowledge/collection")]
    public async Task<bool> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        var options = new CollectionCreateOptions
        {
            DbProvider = request.DbProvider,
            LlmProvider = request.Provider,
            LlmModel = request.Model,
            EmbeddingDimension = request.Dimension
        };
        return await kg.CreateCollection(request.CollectionName, options);
    }

    [HttpDelete("knowledge/collection/{collection}")]
    public async Task<bool> DeleteCollection([FromRoute] string collection, [FromBody] DeleteCollectionRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        return await kg.DeleteCollection(collection, new KnowledgeCollectionOptions { DbProvider = request.DbProvider });
    }

    [HttpPost("/knowledge/collection/{collection}/search")]
    public async Task<IEnumerable<KnowledgeKnowledgeViewModel>> SearchKnowledge([FromRoute] string collection, [FromBody] SearchKnowledgeRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return [];
        }

        var options = BuildSearchOptions(kg, request);
        var results = await kg.ExecuteQuery(request?.Text ?? string.Empty, collection, options);
        return results.Select(x => KnowledgeKnowledgeViewModel.From(x)).ToList();
    }

    [HttpPost("/knowledge/collection/{collection}/data/page")]
    public async Task<StringIdPagedItems<KnowledgeKnowledgeViewModel>> GetPagedCollectionData([FromRoute] string collection, [FromBody] GetPagedCollectionDataRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return new StringIdPagedItems<KnowledgeKnowledgeViewModel>();
        }

        var data = await orchestrator.GetPagedCollectionData(collection, request);
        var items = data.Items?.Select(x => KnowledgeKnowledgeViewModel.From(x))?.ToList() ?? [];

        return new StringIdPagedItems<KnowledgeKnowledgeViewModel>
        {
            Count = data.Count,
            NextId = data.NextId,
            Items = items
        };
    }

    [HttpPost("/knowledge/collection/{collection}/data")]
    public async Task<bool> CreateCollectionData([FromRoute] string collection, [FromBody] KnowledgeDataCreateRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        var create = new KnowledgeCreateModel
        {
            DbProvider = request.DbProvider,
            Text = request.Text,
            Payload = request.Payload
        };

        var created = await kg.CreateCollectionData(collection, create);
        return created;
    }

    [HttpGet("/knowledge/collection/{collection}/data")]
    public async Task<IEnumerable<KnowledgeKnowledgeViewModel>> GetCollectionData([FromRoute] string collection, [FromQuery] string knowledgeType, [FromQuery] QueryCollectionDataRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (kg == null)
        {
            return [];
        }

        var options = new KnowledgeQueryOptions
        {
            DbProvider = request.DbProvider,
            WithPayload = request.WithPayload,
            WithVector = request.WithVector
        };

        var points = await kg.GetCollectionData(collection, request.Ids, options);
        return points.Select(x => KnowledgeKnowledgeViewModel.From(x));
    }

    [HttpPut("/knowledge/collection/{collection}/data")]
    public async Task<bool> UpdateCollectionData([FromRoute] string collection, [FromBody] KnowledgeDataUpdateRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        var update = new KnowledgeUpdateModel
        {
            DbProvider = request.DbProvider,
            Id = request.Id,
            Text = request.Text,
            Payload = request.Payload
        };

        var updated = await kg.UpdateCollectionData(collection, update);
        return updated;
    }

    [HttpDelete("/knowledge/collection/{collection}/data/{id}")]
    public async Task<bool> DeleteCollectionData([FromRoute] string collection, [FromRoute] string id, [FromBody] DeleteCollectionDataRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        return await kg.DeleteCollectionData(collection, id, new KnowledgeCollectionOptions { DbProvider = request.DbProvider });
    }

    [HttpDelete("/knowledge/collection/{collection}/data")]
    public async Task<bool> DeleteCollectionAllData([FromRoute] string collection, [FromBody] DeleteCollectionDataRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        return await kg.DeleteCollectionData(collection, new KnowledgeCollectionOptions { DbProvider = request.DbProvider });
    }
    #endregion


    #region Index
    [HttpPost("/knowledge/collection/{collection}/indexes")]
    public async Task<SuccessFailResponse<string>> CreateCollectionIndexes([FromRoute] string collection, [FromBody] CreateCollectionIndexRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return new();
        }

        var options = new KnowledgeIndexOptions
        {
            DbProvider = request.DbProvider,
            Items = request.Options
        };
        return await kg.CreateIndexes(collection, options);
    }

    [HttpDelete("/knowledge/collection/{collection}/indexes")]
    public async Task<SuccessFailResponse<string>> DeleteCollectionIndexes([FromRoute] string collection, [FromBody] DeleteCollectionIndexRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return new();
        }

        var options = new KnowledgeIndexOptions
        {
            DbProvider = request.DbProvider,
            Items = request.Options
        };
        return await kg.DeleteIndexes(collection, options);
    }
    #endregion


    #region Snapshot
    [HttpGet("/knowledge/collection/{collection}/snapshots")]
    public async Task<IEnumerable<KnowledgeCollectionSnapshotViewModel>> GetCollectionSnapshots([FromRoute] string collection, [FromQuery] string knowledgeType, [FromQuery] string? dbProvider = null)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (kg == null)
        {
            return [];
        }

        var snapshots = await kg.GetCollectionSnapshots(collection, new KnowledgeSnapshotOptions { DbProvider = dbProvider });
        return snapshots.Select(x => KnowledgeCollectionSnapshotViewModel.From(x)!);
    }

    [HttpPost("/knowledge/collection/{collection}/snapshot")]
    public async Task<KnowledgeCollectionSnapshotViewModel?> CreateCollectionSnapshot([FromRoute] string collection, [FromBody] CollectionSnapshotRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return null;
        }

        var snapshot = await kg.CreateCollectionSnapshot(collection, new KnowledgeSnapshotOptions { DbProvider = request.DbProvider });
        return KnowledgeCollectionSnapshotViewModel.From(snapshot);
    }

    [HttpGet("/knowledge/collection/{collection}/snapshot")]
    public async Task<IActionResult> GetCollectionSnapshot([FromRoute] string collection, [FromQuery] string snapshotFileName, [FromQuery] string knowledgeType, [FromQuery] string? dbProvider = null)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (kg == null)
        {
            return BuildFileResult(snapshotFileName, BinaryData.Empty);
        }

        var snapshot = await kg.DownloadCollectionSnapshot(collection, snapshotFileName, new KnowledgeSnapshotOptions { DbProvider = dbProvider });
        return BuildFileResult(snapshotFileName, snapshot);
    }

    [HttpPost("/knowledge/collection/{collection}/snapshot/recover")]
    public async Task<bool> RecoverCollectionFromSnapshot([FromRoute] string collection, IFormFile snapshotFile, [FromForm] string knowledgeType, [FromForm] string? dbProvider = null)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (kg == null)
        {
            return false;
        }

        var fileName = snapshotFile.FileName;
        var binary = FileUtility.BuildBinaryDataFromFile(snapshotFile);
        var done = await kg.RecoverCollectionFromSnapshot(collection, fileName, binary, new KnowledgeSnapshotOptions { DbProvider = dbProvider });
        return done;
    }

    [HttpDelete("/knowledge/collection/{collection}/snapshot")]
    public async Task<bool> DeleteCollectionSnapshots([FromRoute] string collection, [FromBody] DeleteCollectionSnapshotRequest request)
    {
        var kg = _services.GetServices<IKnowledgeOrchestrator>()
                          .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (kg == null)
        {
            return false;
        }

        var done = await kg.DeleteCollectionSnapshot(collection, request.SnapshotName, new KnowledgeSnapshotOptions { DbProvider = request.DbProvider });
        return done;
    }
    #endregion


    #region Graph
    [HttpPost("/knowledge/graph/search")]
    public async Task<GraphKnowledgeViewModel> SearchGraphKnowledge([FromBody] SearchGraphKnowledgeRequest request)
    {
        var options = new GraphQueryOptions
        {
            Provider = request.Provider,
            GraphId = request.GraphId,
            Arguments = request.Arguments,
            Method = request.Method
        };

        var result = await _graphKnowledgeService.ExecuteQueryAsync(request.Query, options);
        return new GraphKnowledgeViewModel
        {
            Result = result.Result
        };
    }
    #endregion


    #region Common
    [HttpPost("/knowledge/refresh-configs")]
    public async Task<bool> RefreshCollectionConfigs([FromBody] KnowledgeCollectionConfigsRequest request)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        if (request.Collections.IsNullOrEmpty())
        {
            return false;
        }
        var saved = await db.AddKnowledgeCollectionConfigs(request.Collections, reset: true);
        return saved;
    }
    #endregion


    #region Private methods
    private FileStreamResult BuildFileResult(string fileName, BinaryData fileData)
    {
        var stream = fileData.ToStream();
        stream.Position = 0;
        return File(stream, "application/octet-stream", Path.GetFileName(fileName));
    }

    private KnowledgeExecuteOptions BuildSearchOptions(IKnowledgeOrchestrator kg, SearchKnowledgeRequest? request)
    {
        var searchParam = request?.SearchParam?.ToDictionary(x => x.Key, x => x.Value?.ConvertToString());

        if (kg.KnowledgeType.IsEqualTo(KnowledgeBaseType.SemanticGraph))
        {
            return new GraphKnowledgeSearchOptions
            {
                DbProvider = request?.DbProvider,
                SearchParam = searchParam,
                SearchArguments = request?.SearchArguments,
                GraphId = request?.GraphId
            };
        }

        if (kg.KnowledgeType.IsEqualTo(KnowledgeBaseType.Taxonomy))
        {
            return new TaxonomyKnowledgeSearchOptions
            {
                DbProvider = request?.DbProvider,
                Limit = request?.Limit ?? 5,
                Confidence = request?.Confidence ?? 0.5f,
                SearchParam = searchParam,
                SearchArguments = request?.SearchArguments,
                DataProviders = request?.DataProviders,
                MaxNgram = request?.MaxNgram
            };
        }

        return new KnowledgeExecuteOptions
        {
            DbProvider = request?.DbProvider,
            Fields = request?.Fields,
            FilterGroups = request?.FilterGroups,
            Limit = request?.Limit ?? 5,
            Confidence = request?.Confidence ?? 0.5f,
            WithVector = request?.WithVector ?? false,
            SearchParam = searchParam,
            SearchArguments = request?.SearchArguments
        };
    }
    #endregion
}
