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
    public async Task<bool> ExistCollection([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        return await orchestrator.ExistCollection(collection, new KnowledgeCollectionOptions());
    }

    [HttpGet("knowledge/collections")]
    public async Task<IEnumerable<KnowledgeCollectionConfigViewModel>> GetCollections([FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return [];
        }

        var collections = await orchestrator.GetCollections(new KnowledgeCollectionOptions());
        return collections.Select(x => KnowledgeCollectionConfigViewModel.From(x));
    }

    [HttpPost("knowledge/collection")]
    public async Task<bool> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        var options = new CollectionCreateOptions
        {
            LlmProvider = request.Provider,
            LlmModel = request.Model,
            EmbeddingDimension = request.Dimension
        };
        return await orchestrator.CreateCollection(request.CollectionName, options);
    }

    [HttpDelete("knowledge/collection/{collection}")]
    public async Task<bool> DeleteCollection([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        return await orchestrator.DeleteCollection(collection, new KnowledgeCollectionOptions());
    }

    [HttpPost("/knowledge/collection/{collection}/search")]
    public async Task<IEnumerable<KnowledgeKnowledgeViewModel>> SearchKnowledge([FromRoute] string collection, [FromBody] SearchKnowledgeRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return [];
        }

        var options = BuildSearchOptions(orchestrator, request);
        var results = await orchestrator.Search(request?.Text ?? string.Empty, collection, options);
        return results.Select(x => KnowledgeKnowledgeViewModel.From(x)).ToList();
    }

    [HttpPost("/knowledge/collection/{collection}/data/page")]
    public async Task<StringIdPagedItems<KnowledgeKnowledgeViewModel>> GetPagedCollectionData([FromRoute] string collection, [FromQuery] string knowledgeType, [FromBody] KnowledgeFilter filter)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return new StringIdPagedItems<KnowledgeKnowledgeViewModel>();
        }

        var data = await orchestrator.GetPagedCollectionData(collection, filter);
        var items = data.Items?.Select(x => KnowledgeKnowledgeViewModel.From(x))?.ToList() ?? [];

        return new StringIdPagedItems<KnowledgeKnowledgeViewModel>
        {
            Count = data.Count,
            NextId = data.NextId,
            Items = items
        };
    }

    [HttpPost("/knowledge/collection/{collection}/data")]
    public async Task<bool> CreateCollectionData([FromRoute] string collection, [FromBody] KnowledgeCreateRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        var create = new KnowledgeCreateModel
        {
            Text = request.Text,
            Payload = request.Payload
        };

        var created = await orchestrator.CreateCollectionData(collection, create);
        return created;
    }

    [HttpGet("/knowledge/collection/{collection}/data")]
    public async Task<IEnumerable<KnowledgeKnowledgeViewModel>> GetCollectionData([FromRoute] string collection, [FromQuery] string knowledgeType, [FromQuery] QueryVectorDataRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return [];
        }

        var options = new KnowledgeQueryOptions
        {
            WithPayload = request.WithPayload,
            WithVector = request.WithVector
        };

        var points = await orchestrator.GetCollectionData(collection, request.Ids, options);
        return points.Select(x => KnowledgeKnowledgeViewModel.From(x));
    }

    [HttpPut("/knowledge/collection/{collection}/data")]
    public async Task<bool> UpdateCollectionData([FromRoute] string collection, [FromBody] KnowledgeUpdateRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        var update = new KnowledgeUpdateModel
        {
            Id = request.Id,
            Text = request.Text,
            Payload = request.Payload
        };

        var updated = await orchestrator.UpdateCollectionData(collection, update);
        return updated;
    }

    [HttpDelete("/knowledge/collection/{collection}/data/{id}")]
    public async Task<bool> DeleteCollectionData([FromRoute] string collection, [FromRoute] string id, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        return await orchestrator.DeleteCollectionData(collection, id, new KnowledgeCollectionOptions());
    }

    [HttpDelete("/knowledge/collection/{collection}/data")]
    public async Task<bool> DeleteCollectionAllData([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        return await orchestrator.DeleteCollectionData(collection, new KnowledgeCollectionOptions());
    }
    #endregion


    #region Index
    [HttpPost("/knowledge/collection/{collection}/indexes")]
    public async Task<SuccessFailResponse<string>> CreateCollectionIndexes([FromRoute] string collection, [FromBody] CreateCollectionIndexRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return new();
        }

        var options = new KnowledgeIndexOptions
        {
            Items = request.Options
        };
        return await orchestrator.CreateIndexes(collection, options);
    }

    [HttpDelete("/knowledge/collection/{collection}/indexes")]
    public async Task<SuccessFailResponse<string>> DeleteCollectionIndexes([FromRoute] string collection, [FromBody] DeleteCollectionIndexRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return new();
        }

        var options = new KnowledgeIndexOptions
        {
            Items = request.Options
        };
        return await orchestrator.DeleteIndexes(collection, options);
    }
    #endregion


    #region Snapshot
    [HttpGet("/knowledge/collection/{collection}/snapshots")]
    public async Task<IEnumerable<KnowledgeCollectionSnapshotViewModel>> GetCollectionSnapshots([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return [];
        }

        var snapshots = await orchestrator.GetCollectionSnapshots(collection);
        return snapshots.Select(x => KnowledgeCollectionSnapshotViewModel.From(x)!);
    }

    [HttpPost("/knowledge/collection/{collection}/snapshot")]
    public async Task<KnowledgeCollectionSnapshotViewModel?> CreateCollectionSnapshot([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return null;
        }

        var snapshot = await orchestrator.CreateCollectionSnapshot(collection);
        return KnowledgeCollectionSnapshotViewModel.From(snapshot);
    }

    [HttpGet("/knowledge/collection/{collection}/snapshot")]
    public async Task<IActionResult> GetCollectionSnapshot([FromRoute] string collection, [FromQuery] string snapshotFileName, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return BuildFileResult(snapshotFileName, BinaryData.Empty);
        }

        var snapshot = await orchestrator.DownloadCollectionSnapshot(collection, snapshotFileName);
        return BuildFileResult(snapshotFileName, snapshot);
    }

    [HttpPost("/knowledge/collection/{collection}/snapshot/recover")]
    public async Task<bool> RecoverCollectionFromSnapshot([FromRoute] string collection, IFormFile snapshotFile, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        var fileName = snapshotFile.FileName;
        var binary = FileUtility.BuildBinaryDataFromFile(snapshotFile);
        var done = await orchestrator.RecoverCollectionFromSnapshot(collection, fileName, binary);
        return done;
    }

    [HttpDelete("/knowledge/collection/{collection}/snapshot")]
    public async Task<bool> DeleteCollectionSnapshots([FromRoute] string collection, [FromBody] DeleteCollectionSnapshotRequest request)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(request.KnowledgeType));

        if (orchestrator == null)
        {
            return false;
        }

        var done = await orchestrator.DeleteCollectionSnapshot(collection, request.SnapshotName);
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

    private KnowledgeSearchOptions BuildSearchOptions(IKnowledgeOrchestrator orchestrator, SearchKnowledgeRequest? request)
    {
        if (orchestrator.KnowledgeType.IsEqualTo(KnowledgeBaseType.SemanticGraph))
        {
            return new GraphKnowledgeSearchOptions
            {
                DbProvider = request?.DbProvider,
                SearchParam = request?.SearchParam,
                SearchArguments = request?.SearchArguments,
                GraphId = request?.GraphId
            };
        }

        if (orchestrator.KnowledgeType.IsEqualTo(KnowledgeBaseType.Taxonomy))
        {
            return new TaxonomyKnowledgeSearchOptions
            {
                DbProvider = request?.DbProvider,
                Limit = request?.Limit ?? 5,
                Confidence = request?.Confidence ?? 0.5f,
                SearchParam = request?.SearchParam,
                SearchArguments = request?.SearchArguments,
                DataProviders = request?.DataProviders,
                MaxNgram = request?.MaxNgram
            };
        }

        return new KnowledgeSearchOptions
        {
            DbProvider = request?.DbProvider,
            Fields = request?.Fields,
            FilterGroups = request?.FilterGroups,
            Limit = request?.Limit ?? 5,
            Confidence = request?.Confidence ?? 0.5f,
            WithVector = request?.WithVector ?? false,
            SearchParam = request?.SearchParam,
            SearchArguments = request?.SearchArguments
        };
    }
    #endregion
}
