namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class FileController : ControllerBase
{
    private readonly IServiceProvider _services;

    public FileController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("/conversation/{conversationId}/attachments")]
    public IActionResult UploadAttachments([FromRoute] string conversationId,
        IFormFile[] files)
    {
        if (files != null && files.Length > 0)
        {
            var fileService = _services.GetRequiredService<IBotSharpFileService>();
            var dir = fileService.GetDirectory(conversationId);
            foreach (var file in files)
            {
                // Save the file, process it, etc.
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var filePath = Path.Combine(dir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
            }

            return Ok(new { message = "File uploaded successfully." });
        }

        return BadRequest(new { message = "Invalid file." });
    }

    [HttpGet("/conversation/{conversationId}/files/{messageId}")]
    public IEnumerable<MessageFileViewModel> GetMessageFiles([FromRoute] string conversationId, [FromRoute] string messageId)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var files = fileService.GetMessageFiles(conversationId, new List<string> { messageId });
        return files?.Select(x => MessageFileViewModel.Transform(x))?.ToList() ?? new List<MessageFileViewModel>();
    }

    [HttpGet("/conversation/{conversationId}/message/{messageId}/file/{fileName}")]
    public async Task<IActionResult> GetMessageFile([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string fileName)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var file = fileService.GetMessageFile(conversationId, messageId, fileName);
        if (string.IsNullOrEmpty(file))
        {
            return NotFound();
        }

        using Stream stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        return File(bytes, "application/octet-stream", Path.GetFileName(file));
    }
}
