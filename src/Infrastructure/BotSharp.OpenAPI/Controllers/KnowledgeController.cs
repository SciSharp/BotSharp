using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Knowledges.Models;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;
using BotSharp.Core.Plugins.Knowledges;
using BotSharp.Abstraction.Knowledges.Settings;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class KnowledgeController : ControllerBase, IApiAdapter
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IServiceProvider _services;

    public KnowledgeController(IKnowledgeService knowledgeService, IServiceProvider services)
    {
        _knowledgeService = knowledgeService;
        _services = services;
    }
    [HttpGet("/knowledge/{agentId}")]
    public async Task<List<RetrievedResult>> RetrieveKnowledge([FromRoute] string agentId, [FromQuery(Name = "q")] string question)
    {
        return await _knowledgeService.GetAnswer(new KnowledgeRetrievalModel
        {
            AgentId = agentId,
            Question = question
        });
    }

    [HttpPost("/knowledge/{agentId}")]
    public async Task<IActionResult> FeedKnowledge([FromRoute] string agentId, List<IFormFile> files, [FromQuery] int? startPageNum, [FromQuery] int? endPageNum, [FromQuery] bool? paddleModel)
    {
        var setttings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var textConverter = _services.GetServices<IPdf2TextConverter>().First(x => x.GetType().FullName.EndsWith(setttings.Pdf2TextConverter));
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            var content = "";

            content = await textConverter.ConvertPdfToText(formFile, startPageNum, endPageNum);

            // Process uploaded files
            // Don't rely on or trust the FileName property without validation.

            // Add FeedWithMetaData
            await _knowledgeService.Feed(new KnowledgeFeedModel
            {
                AgentId = agentId,
                Content = content
            });
        }

        return Ok(new { count = files.Count, size });
    }
}
