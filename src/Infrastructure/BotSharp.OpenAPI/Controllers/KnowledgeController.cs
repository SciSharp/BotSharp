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
    public KnowledgeController(IKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
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
    public async Task<IActionResult> FeedKnowledge([FromRoute] string agentId, List<IFormFile> files, [FromQuery] int? startPageNum, [FromQuery] int? endPageNum)
    {
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            if (formFile.Length <= 0)
            {
                continue;
            }

            var filePath = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(filePath))
            {
                await formFile.CopyToAsync(stream);
            }

            var document = PdfDocument.Open(filePath);
            var content = "";
            foreach (Page page in document.GetPages())
            {
                if (startPageNum.HasValue && page.Number < startPageNum.Value)
                {
                    continue;
                }

                if (endPageNum.HasValue && page.Number > endPageNum.Value)
                {
                    continue;
                }

                content += page.Text;
            }

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
