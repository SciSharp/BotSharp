using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Knowledges.Processors;
using BotSharp.Abstraction.Knowledges.Responses;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
    #region File
    [HttpGet("/knowledge/processors")]
    public IEnumerable<string> GetKnowledgeProcessors()
    {
        return _services.GetServices<IKnowledgeProcessor>().Select(x => x.Provider);
    }

    [HttpPost("/knowledge/collection/{collection}/file/upload")]
    public async Task<UploadKnowledgeResponse> UploadKnowledgeFiles([FromRoute] string collection, [FromBody] KnowledgeUploadRequest request)
    {
        var fileOrchestrator = GetKnowledgeFileOrchestrator(request.Orchestrator);
        var response = await fileOrchestrator.UploadFilesToKnowledge(collection, request.Files, request.Options);
        return response;
    }

    [HttpPost("/knowledge/collection/{collection}/file/form")]
    public async Task<UploadKnowledgeResponse> UploadKnowledgeFiles(
        [FromRoute] string collection,
        [FromForm] IEnumerable<IFormFile> files,
        [FromForm] string? orchestrator = null,
        [FromForm] KnowledgeFileHandleOptions? options = null)
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

        var fileOrchestrator = GetKnowledgeFileOrchestrator(orchestrator);
        var response = await fileOrchestrator.UploadFilesToKnowledge(collection, docs, options);
        return response;
    }

    [HttpDelete("/knowledge/collection/{collection}/file/{fileId}")]
    public async Task<bool> DeleteKnowledgeFile([FromRoute] string collection, [FromRoute] Guid fileId, [FromQuery] KnowledgeFileRequest? request = null)
    {
        var fileOrchestrator = GetKnowledgeFileOrchestrator(request?.Orchestrator);
        var options = !string.IsNullOrWhiteSpace(request?.DbProvider) ? new KnowledgeFileOptions { DbProvider = request.DbProvider } : null;
        var response = await fileOrchestrator.DeleteKnowledgeFile(collection, fileId, options);
        return response;
    }

    [HttpDelete("/knowledge/collection/{collection}/file")]
    public async Task<bool> DeleteKnowledgeFiles([FromRoute] string collection, [FromBody] GetKnowledgeFilesRequest request)
    {
        var fileOrchestrator = GetKnowledgeFileOrchestrator(request.Orchestrator);
        var response = await fileOrchestrator.DeleteKnowledgeFiles(collection, request);
        return response;
    }

    [HttpPost("/knowledge/collection/{collection}/file/page")]
    public async Task<PagedItems<KnowledgeFileViewModel>> GetPagedKnowledgeFiles([FromRoute] string collection, [FromBody] GetKnowledgeFilesRequest request)
    {
        var fileOrchestrator = GetKnowledgeFileOrchestrator(request.Orchestrator);
        var data = await fileOrchestrator.GetPagedKnowledgeFiles(collection, request);

        return new PagedItems<KnowledgeFileViewModel>
        {
            Items = data.Items.Select(x => KnowledgeFileViewModel.From(x)),
            Count = data.Count
        };
    }

    [HttpGet("/knowledge/collection/{collection}/file/{fileId}")]
    public async Task<IActionResult> GetKnowledgeFile([FromRoute] string collection, [FromRoute] Guid fileId, [FromQuery] KnowledgeFileRequest? request = null)
    {
        var fileOrchestrator = GetKnowledgeFileOrchestrator(request?.Orchestrator);
        var options = !string.IsNullOrWhiteSpace(request?.DbProvider) ? new KnowledgeFileOptions { DbProvider = request.DbProvider } : null;
        var file = await fileOrchestrator.GetKnowledgeFileBinaryData(collection, fileId, options);
        var stream = file.FileBinaryData.ToStream();
        stream.Position = 0;

        return new FileStreamResult(stream, file.ContentType) { FileDownloadName = file.FileName };
    }

    private IKnowledgeFileOrchestrator? GetKnowledgeFileOrchestrator(string? provider)
    {
        provider ??= "botsharp-knowledge-doc";
        var found = _services.GetServices<IKnowledgeFileOrchestrator>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        return found;
    }
    #endregion
}
