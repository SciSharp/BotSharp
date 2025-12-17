using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Processors;
using BotSharp.Abstraction.Knowledges.Responses;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
    #region Document
    [HttpGet("/knowledge/document/processors")]
    public IEnumerable<string> GetKnowledgeDocumentProcessors()
    {
        return _services.GetServices<IKnowledgeProcessor>().Select(x => x.Provider);
    }

    [HttpPost("/knowledge/document/{collection}/upload")]
    public async Task<UploadKnowledgeResponse> UploadKnowledgeDocuments([FromRoute] string collection, [FromBody] VectorKnowledgeUploadRequest request)
    {
        var response = await _knowledgeService.UploadDocumentsToKnowledge(collection, request.Files, request.Options);
        return response;
    }

    [HttpPost("/knowledge/document/{collection}/form")]
    public async Task<UploadKnowledgeResponse> UploadKnowledgeDocuments(
        [FromRoute] string collection,
        [FromForm] IEnumerable<IFormFile> files,
        [FromForm] KnowledgeDocOptions? options = null)
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

        var response = await _knowledgeService.UploadDocumentsToKnowledge(collection, docs, options);
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
}
