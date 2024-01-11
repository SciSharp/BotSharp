using Microsoft.AspNetCore.Hosting;
using SharpCompress.Compressors.Xz;
using System;
using System.IO;
using System.Net.Mime;

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
}
