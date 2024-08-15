using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IServiceProvider _services;

    public KnowledgeBaseController(IKnowledgeService knowledgeService, IServiceProvider services)
    {
        _knowledgeService = knowledgeService;
        _services = services;
    }

    [HttpGet("knowledge/collections")]
    public async Task<IEnumerable<string>> GetKnowledgeCollections()
    {
        return await _knowledgeService.GetKnowledgeCollections();
    }

    [HttpPost("/knowledge/{collection}/search")]
    public async Task<IEnumerable<KnowledgeSearchResultViewModel>> SearchKnowledge([FromRoute] string collection, [FromBody] SearchKnowledgeRequest request)
    {
        var options = new KnowledgeSearchOptions
        {
            Text = request.Text,
            Fields = request.Fields,
            Limit = request.Limit ?? 5,
            Confidence = request.Confidence ?? 0.5f,
            WithVector = request.WithVector
        };

        var results = await _knowledgeService.SearchKnowledge(collection, options);
        return results.Select(x => KnowledgeSearchResultViewModel.From(x)).ToList();
    }

    [HttpPost("/knowledge/{collection}/data")]
    public async Task<StringIdPagedItems<KnowledgeSearchResultViewModel>> GetKnowledgeCollectionData([FromRoute] string collection, [FromBody] KnowledgeFilter filter)
    {
        var data = await _knowledgeService.GetKnowledgeCollectionData(collection, filter);
        var items = data.Items?.Select(x => KnowledgeSearchResultViewModel.From(x))?
                               .ToList() ?? new List<KnowledgeSearchResultViewModel>();

        return new StringIdPagedItems<KnowledgeSearchResultViewModel>
        {
            Count = data.Count,
            NextId = data.NextId,
            Items = items
        };
    }

    [HttpDelete("/knowledge/{collection}/data/{id}")]
    public async Task<bool> DeleteKnowledgeCollectionData([FromRoute] string collection, [FromRoute] string id)
    {
        return await _knowledgeService.DeleteKnowledgeCollectionData(collection, id);
    }

    [HttpPost("/knowledge/{collection}/upload")]
    public async Task<IActionResult> UploadKnowledge([FromRoute] string collection, IFormFile file, [FromForm] int? startPageNum, [FromForm] int? endPageNum)
    {
        var setttings = _services.GetRequiredService<FileCoreSettings>();
        var textConverter = _services.GetServices<IPdf2TextConverter>().FirstOrDefault(x => x.Name == setttings.Pdf2TextConverter);

        var filePath = Path.GetTempFileName();
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
            await stream.FlushAsync();
        }

        var content = await textConverter.ConvertPdfToText(filePath, startPageNum, endPageNum);
        await _knowledgeService.FeedKnowledge(collection, new KnowledgeCreationModel
        {
            Content = content
        });

        System.IO.File.Delete(filePath);
        return Ok(new { count = 1, file.Length });
    }
}
