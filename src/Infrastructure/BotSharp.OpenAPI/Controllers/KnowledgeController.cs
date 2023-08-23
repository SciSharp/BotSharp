using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Knowledges.Models;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class KnowledgeController : ControllerBase, IApiAdapter
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IPdf2TextConverter _pdf2TextConverter;
    public KnowledgeController(IKnowledgeService knowledgeService, IPdf2TextConverter pdf2TextConverter)
    {
        _knowledgeService = knowledgeService;
        _pdf2TextConverter = pdf2TextConverter;
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
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            var content = "";
            
            content = await _pdf2TextConverter.ConvertPdfToText(formFile, startPageNum, endPageNum);

            // Process uploaded files
            // Don't rely on or trust the FileName property without validation.
            await _knowledgeService.Feed(new KnowledgeFeedModel
            {
                AgentId = agentId,
                Content = content
            });
        }

        return Ok(new { count = files.Count, size });
    }
}
