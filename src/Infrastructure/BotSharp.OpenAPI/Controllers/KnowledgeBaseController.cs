using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.VectorStorage.Models;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IServiceProvider _services;

    public KnowledgeBaseController(
        IKnowledgeService knowledgeService,
        IServiceProvider services)
    {
        _knowledgeService = knowledgeService;
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

    [HttpPost("knowledge/vector/create-collection")]
    public async Task<bool> CreateVectorCollection([FromBody] CreateVectorCollectionRequest request)
    {
        return await _knowledgeService.CreateVectorCollection(request.CollectionName, request.CollectionType, request.Dimension, request.Provider, request.Model);
    }

    [HttpDelete("knowledge/vector/{collection}/delete-collection")]
    public async Task<bool> DeleteVectorCollection([FromRoute] string collection)
    {
        return await _knowledgeService.DeleteVectorCollection(collection);
    }

    [HttpPost("/knowledge/vector/{collection}/search")]
    public async Task<IEnumerable<VectorKnowledgeViewModel>> SearchVectorKnowledge([FromRoute] string collection, [FromBody] SearchVectorKnowledgeRequest request)
    {
        var options = new VectorSearchOptions
        {
            Fields = request.Fields,
            Limit = request.Limit ?? 5,
            Confidence = request.Confidence ?? 0.5f,
            WithVector = request.WithVector
        };

        var results = await _knowledgeService.SearchVectorKnowledge(request.Text, collection, options);
        return results.Select(x => VectorKnowledgeViewModel.From(x)).ToList();
    }

    [HttpPost("/knowledge/vector/{collection}/page")]
    public async Task<StringIdPagedItems<VectorKnowledgeViewModel>> GetPagedVectorCollectionData([FromRoute] string collection, [FromBody] VectorFilter filter)
    {
        var data = await _knowledgeService.GetPagedVectorCollectionData(collection, filter);
        var items = data.Items?.Select(x => VectorKnowledgeViewModel.From(x))?
                               .ToList() ?? new List<VectorKnowledgeViewModel>();

        return new StringIdPagedItems<VectorKnowledgeViewModel>
        {
            Count = data.Count,
            NextId = data.NextId,
            Items = items
        };
    }

    [HttpPost("/knowledge/vector/{collection}/create")]
    public async Task<bool> CreateVectorKnowledge([FromRoute] string collection, [FromBody] VectorKnowledgeCreateRequest request)
    {
        var create = new VectorCreateModel
        {
            Text = request.Text,
            DataSource = request.DataSource,
            Payload = request.Payload
        };

        var created = await _knowledgeService.CreateVectorCollectionData(collection, create);
        return created;
    }

    [HttpPut("/knowledge/vector/{collection}/update")]
    public async Task<bool> UpdateVectorKnowledge([FromRoute] string collection, [FromBody] VectorKnowledgeUpdateRequest request)
    {
        var update = new VectorUpdateModel
        {
            Id = request.Id,
            Text = request.Text,
            DataSource = request.DataSource,
            Payload = request.Payload
        };

        var updated = await _knowledgeService.UpdateVectorCollectionData(collection, update);
        return updated;
    }

    [HttpDelete("/knowledge/vector/{collection}/data/{id}")]
    public async Task<bool> DeleteVectorCollectionData([FromRoute] string collection, [FromRoute] string id)
    {
        return await _knowledgeService.DeleteVectorCollectionData(collection, id);
    }

    [HttpDelete("/knowledge/vector/{collection}/data")]
    public async Task<bool> DeleteVectorCollectionAllData([FromRoute] string collection)
    {
        return await _knowledgeService.DeleteVectorCollectionAllData(collection);
    }
    #endregion


    #region Snapshot
    [HttpGet("/knowledge/vector/{collection}/snapshots")]
    public async Task<IEnumerable<VectorCollectionSnapshotViewModel>> GetVectorCollectionSnapshots([FromRoute] string collection)
    {
        var snapshots = await _knowledgeService.GetVectorCollectionSnapshots(collection);
        return snapshots.Select(x => VectorCollectionSnapshotViewModel.From(x));
    }

    [HttpPost("/knowledge/vector/{collection}/snapshot")]
    public async Task<VectorCollectionSnapshotViewModel?> CreateVectorCollectionSnapshot([FromRoute] string collection)
    {
        var snapshot = await _knowledgeService.CreateVectorCollectionSnapshot(collection);
        return VectorCollectionSnapshotViewModel.From(snapshot);
    }

    [HttpGet("/knowledge/vector/{collection}/snapshot")]
    public async Task<IActionResult> GetVectorCollectionSnapshot([FromRoute] string collection, [FromQuery] string snapshotFileName)
    {
        var snapshot = await _knowledgeService.DownloadVectorCollectionSnapshot(collection, snapshotFileName);
        return BuildFileResult(snapshotFileName, snapshot);
    }

    [HttpPost("/knowledge/vector/{collection}/snapshot/recover")]
    public async Task<bool> RecoverVectorCollectionFromSnapshot([FromRoute] string collection, IFormFile snapshotFile)
    {
        var fileName = snapshotFile.FileName;
        var binary = FileUtility.BuildBinaryDataFromFile(snapshotFile);
        var done = await _knowledgeService.RecoverVectorCollectionFromSnapshot(collection, fileName, binary);
        return done;
    }

    [HttpDelete("/knowledge/vector/{collection}/snapshot")]
    public async Task<bool> DeleteVectorCollectionSnapshots([FromRoute] string collection, [FromBody] DeleteVectorCollectionSnapshotRequest request)
    {
        var done = await _knowledgeService.DeleteVectorCollectionSnapshot(collection, request.SnapshotName);
        return done;
    }
    #endregion


    #region Document
    [HttpPost("/knowledge/document/{collection}/upload")]
    public async Task<UploadKnowledgeResponse> UploadKnowledgeDocuments([FromRoute] string collection, [FromBody] VectorKnowledgeUploadRequest request)
    {
        var response = await _knowledgeService.UploadDocumentsToKnowledge(collection, request.Files, request.ChunkOption);
        return response;
    }

    [HttpPost("/knowledge/document/{collection}/form-upload")]
    public async Task<UploadKnowledgeResponse> UploadKnowledgeDocuments([FromRoute] string collection,
        [FromForm] IEnumerable<IFormFile> files, [FromForm] ChunkOption? option = null)
    {
        if (files.IsNullOrEmpty())
        {
            return new UploadKnowledgeResponse();
        }

        var docs = new List<ExternalFileModel>();
        foreach (var file in files)
        {
            var data = FileUtility.BuildFileDataFromFile(file);
            docs.Add(new ExternalFileModel
            {
                FileName = file.FileName,
                FileData = data
            });
        }

        var response = await _knowledgeService.UploadDocumentsToKnowledge(collection, docs, option);
        return response;
    }

    [HttpDelete("/knowledge/document/{collection}/delete/{fileId}")]
    public async Task<bool> DeleteKnowledgeDocument([FromRoute] string collection, [FromRoute] Guid fileId)
    {
        var response = await _knowledgeService.DeleteKnowledgeDocument(collection, fileId);
        return response;
    }

    [HttpDelete("/knowledge/document/{collection}/delete")]
    public async Task<bool> DeleteKnowledgeDocuments([FromRoute] string collection, [FromBody] GetKnowledgeDocsRequest request)
    {
        var response = await _knowledgeService.DeleteKnowledgeDocuments(collection, request);
        return response;
    }

    [HttpPost("/knowledge/document/{collection}/page")]
    public async Task<PagedItems<KnowledgeFileViewModel>> GetPagedKnowledgeDocuments([FromRoute] string collection, [FromBody] GetKnowledgeDocsRequest request)
    {
        var data = await _knowledgeService.GetPagedKnowledgeDocuments(collection, request);

        return new PagedItems<KnowledgeFileViewModel>
        {
            Items = data.Items.Select(x => KnowledgeFileViewModel.From(x)),
            Count = data.Count
        };
    }

    [HttpGet("/knowledge/document/{collection}/file/{fileId}")]
    public async Task<IActionResult> GetKnowledgeDocument([FromRoute] string collection, [FromRoute] Guid fileId)
    {
        var file = await _knowledgeService.GetKnowledgeDocumentBinaryData(collection, fileId);
        var stream = file.FileBinaryData.ToStream();
        stream.Position = 0;

        return new FileStreamResult(stream, file.ContentType) { FileDownloadName = file.FileName };
    }
    #endregion


    #region Graph
    [HttpPost("/knowledge/graph/search")]
    public async Task<GraphKnowledgeViewModel> SearchGraphKnowledge([FromBody] SearchGraphKnowledgeRequest request)
    {
        var options = new GraphSearchOptions
        {
            Method = request.Method
        };

        var result = await _knowledgeService.SearchGraphKnowledge(request.Query, options);
        return new GraphKnowledgeViewModel
        {
            Result = result.Result
        };
    }
    #endregion


    #region Common
    [HttpPost("/knowledge/vector/refresh-configs")]
    public async Task<string> RefreshVectorCollectionConfigs([FromBody] VectorCollectionConfigsModel request)
    {
        var saved = await _knowledgeService.RefreshVectorKnowledgeConfigs(request);
        return saved ? "Success" : "Fail";
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
