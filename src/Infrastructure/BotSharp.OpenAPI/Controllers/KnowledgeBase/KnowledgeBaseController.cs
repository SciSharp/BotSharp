using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.VectorStorage.Models;
using BotSharp.Abstraction.VectorStorage.Options;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public partial class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IGraphKnowledgeService _graphKnowledgeService;
    private readonly IServiceProvider _services;

    public KnowledgeBaseController(
        IKnowledgeService knowledgeService,
        IGraphKnowledgeService graphKnowledgeService,
        IServiceProvider services)
    {
        _knowledgeService = knowledgeService;
        _graphKnowledgeService = graphKnowledgeService;
        _services = services;
    }

    #region Vector
    [HttpGet("knowledge/vector/{collection}/exist")]
    public async Task<bool> ExistVectorCollection([FromRoute] string collection)
    {
        return await _knowledgeService.ExistVectorCollection(collection);
    }

    [HttpGet("knowledge/vector/collections")]
    public async Task<IEnumerable<VectorCollectionConfigViewModel>> GetVectorCollections([FromQuery] string? type = null)
    {
        var collections = await _knowledgeService.GetVectorCollections(type);
        return collections.Select(x => VectorCollectionConfigViewModel.From(x));
    }

    [HttpGet("knowledge/vector/{collection}/details")]
    public async Task<VectorCollectionDetailsViewModel?> GetVectorCollectionDetails([FromRoute] string collection)
    {
        var details = await _knowledgeService.GetVectorCollectionDetails(collection);
        return VectorCollectionDetailsViewModel.From(details);
    }

    [HttpPost("knowledge/vector/create-collection")]
    public async Task<bool> CreateVectorCollection([FromBody] CreateCollectionRequest request)
    {
        var options = new VectorCollectionCreateOptions
        {
            Provider = request.Provider,
            Model = request.Model,
            Dimension = request.Dimension
        };
        return await _knowledgeService.CreateVectorCollection(request.CollectionName, request.CollectionType, options);
    }

    [HttpDelete("knowledge/vector/{collection}/delete-collection")]
    public async Task<bool> DeleteVectorCollection([FromRoute] string collection)
    {
        return await _knowledgeService.DeleteVectorCollection(collection);
    }

    [HttpPost("/knowledge/vector/{collection}/search")]
    public async Task<IEnumerable<VectorKnowledgeViewModel>> SearchKnowledge([FromRoute] string collection, [FromBody] SearchVectorKnowledgeRequest request)
    {
        var options = new VectorSearchOptions
        {
            Fields = request?.Fields,
            FilterGroups = request?.FilterGroups,
            Limit = request?.Limit ?? 5,
            Confidence = request?.Confidence ?? 0.5f,
            WithVector = request?.WithVector ?? false,
            SearchParam = request?.SearchParam
        };

        var results = await _knowledgeService.SearchVectorKnowledge(request?.Text ?? string.Empty, collection, options);
        return results.Select(x => VectorKnowledgeViewModel.From(x)).ToList();
    }

    [HttpPost("/knowledge/vector/{collection}/page")]
    public async Task<StringIdPagedItems<VectorKnowledgeViewModel>> GetPagedCollectionData([FromRoute] string collection, [FromBody] VectorFilter filter)
    {
        var data = await _knowledgeService.GetPagedVectorCollectionData(collection, filter);
        var items = data.Items?.Select(x => VectorKnowledgeViewModel.From(x))?.ToList() ?? [];

        return new StringIdPagedItems<VectorKnowledgeViewModel>
        {
            Count = data.Count,
            NextId = data.NextId,
            Items = items
        };
    }

    [HttpPost("/knowledge/vector/{collection}/create")]
    public async Task<bool> CreateVectorKnowledge([FromRoute] string collection, [FromBody] KnowledgeCreateRequest request)
    {
        var create = new VectorCreateModel
        {
            Text = request.Text,
            Payload = request.Payload
        };

        var created = await _knowledgeService.CreateVectorCollectionData(collection, create);
        return created;
    }

    [HttpGet("/knowledge/vector/{collection}/points")]
    public async Task<IEnumerable<VectorKnowledgeViewModel>> GetCollectionData([FromRoute] string collection, [FromQuery] QueryVectorDataRequest request)
    {
        var options = new VectorQueryOptions
        {
            WithPayload = request.WithPayload,
            WithVector = request.WithVector
        };

        var points = await _knowledgeService.GetVectorCollectionData(collection, request.Ids, options);
        return points.Select(x => VectorKnowledgeViewModel.From(x));
    }

    [HttpPut("/knowledge/vector/{collection}/update")]
    public async Task<bool> UpdateCollectionData([FromRoute] string collection, [FromBody] KnowledgeUpdateRequest request)
    {
        var update = new VectorUpdateModel
        {
            Id = request.Id,
            Text = request.Text,
            Payload = request.Payload
        };

        var updated = await _knowledgeService.UpdateVectorCollectionData(collection, update);
        return updated;
    }

    [HttpDelete("/knowledge/vector/{collection}/data/{id}")]
    public async Task<bool> DeleteCollectionData([FromRoute] string collection, [FromRoute] string id)
    {
        return await _knowledgeService.DeleteVectorCollectionData(collection, id);
    }

    [HttpDelete("/knowledge/vector/{collection}/data")]
    public async Task<bool> DeleteCollectionAllData([FromRoute] string collection)
    {
        return await _knowledgeService.DeleteVectorCollectionAllData(collection);
    }
    #endregion


    #region Index
    [HttpPost("/knowledge/{collection}/indexes")]
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

    [HttpDelete("/knowledge/{collection}/indexes")]
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
    [HttpGet("/knowledge/{collection}/snapshots")]
    public async Task<IEnumerable<VectorCollectionSnapshotViewModel>> GetCollectionSnapshots([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return [];
        }

        var snapshots = await orchestrator.GetCollectionSnapshots(collection);
        return snapshots.Select(x => VectorCollectionSnapshotViewModel.From(x)!);
    }

    [HttpPost("/knowledge/{collection}/snapshot")]
    public async Task<VectorCollectionSnapshotViewModel?> CreateCollectionSnapshot([FromRoute] string collection, [FromQuery] string knowledgeType)
    {
        var orchestrator = _services.GetServices<IKnowledgeOrchestrator>()
                                    .FirstOrDefault(x => x.KnowledgeType.IsEqualTo(knowledgeType));

        if (orchestrator == null)
        {
            return null;
        }

        var snapshot = await orchestrator.CreateCollectionSnapshot(collection);
        return VectorCollectionSnapshotViewModel.From(snapshot);
    }

    [HttpGet("/knowledge/{collection}/snapshot")]
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

    [HttpPost("/knowledge/{collection}/snapshot/recover")]
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

    [HttpDelete("/knowledge/{collection}/snapshot")]
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
    #endregion
}
