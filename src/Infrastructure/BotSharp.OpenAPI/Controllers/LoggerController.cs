using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Loggers.Services;
using BotSharp.OpenAPI.ViewModels.Instructs;
using Microsoft.AspNetCore.Hosting;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class LoggerController : ControllerBase
{
    private readonly IServiceProvider _services;

    public LoggerController(
        IServiceProvider services)
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

    #region Conversation log
    [HttpGet("/logger/conversation/{conversationId}/content-log")]
    public async Task<DateTimePagination<ContentLogOutputModel>> GetConversationContentLogs(
        [FromRoute] string conversationId,
        [FromQuery] ConversationLogFilter request)
    {
        var logging = _services.GetRequiredService<ILoggerService>();
        return await logging.GetConversationContentLogs(conversationId, request);
    }

    [HttpGet("/logger/conversation/{conversationId}/state-log")]
    public async Task<DateTimePagination<ConversationStateLogModel>> GetConversationStateLogs(
        [FromRoute] string conversationId,
        [FromQuery] ConversationLogFilter request)
    {
        var logging = _services.GetRequiredService<ILoggerService>();
        return await logging.GetConversationStateLogs(conversationId, request);
    }
    #endregion

    #region Instruction log
    [HttpGet("/logger/instruction/log")]
    public async Task<PagedItems<InstructionLogViewModel>> GetInstructionLogs([FromQuery] InstructLogFilter request)
    {
        var logging = _services.GetRequiredService<ILoggerService>();
        var logs = await logging.GetInstructionLogs(request);

        return new PagedItems<InstructionLogViewModel>
        {
            Items = logs.Items.Select(x => InstructionLogViewModel.From(x)),
            Count = logs.Count
        };
    }

    [HttpGet("/logger/instruction/log/keys")]
    public async Task<List<string>> GetInstructionLogKeys([FromQuery] InstructLogKeysFilter request)
    {
        var logging = _services.GetRequiredService<ILoggerService>();
        var keys = await logging.GetInstructionLogSearchKeys(request);
        return keys;
    }
    #endregion
}
