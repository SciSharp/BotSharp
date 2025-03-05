using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.OpenAPI.ViewModels.Instructs;
using Microsoft.AspNetCore.Hosting;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class LoggerController : ControllerBase
{
    private readonly IServiceProvider _services;

    public LoggerController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpGet("/logger/full-log")]
    public async Task<IActionResult> GetFullLog()
    {
        var env = _services.GetRequiredService<IWebHostEnvironment>();
        var logFile = Path.Combine(env.ContentRootPath, "logs", $"log-{DateTime.Now:yyyyMMdd}.txt");
        if (System.IO.File.Exists(logFile))
        {
            using Stream stream = System.IO.File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            return File(bytes, "application/octet-stream", Path.GetFileName(logFile));
        }
        else
        {
            return NotFound();
        }
    }

    [HttpGet("/logger/conversation/{conversationId}/content-log")]
    public async Task<List<ContentLogOutputModel>> GetConversationContentLogs([FromRoute] string conversationId)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        return await conversationService.GetConversationContentLogs(conversationId);
    }

    [HttpGet("/logger/conversation/{conversationId}/state-log")]
    public async Task<List<ConversationStateLogModel>> GetConversationStateLogs([FromRoute] string conversationId)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        return await conversationService.GetConversationStateLogs(conversationId);
    }

    [HttpGet("/logger/instruction/log")]
    public async Task<PagedItems<InstructionLogViewModel>> GetInstructionLogs([FromQuery] InstructLogFilter request)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = db.GetInstructionLogs(request);
        return new PagedItems<InstructionLogViewModel>
        {
            Items = logs.Items.Select(x => InstructionLogViewModel.From(x)),
            Count = logs.Count
        };
    }
}
