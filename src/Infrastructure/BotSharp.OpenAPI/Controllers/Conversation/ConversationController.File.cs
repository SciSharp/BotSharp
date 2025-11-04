using BotSharp.Abstraction.Files.Enums;
using BotSharp.Abstraction.Files.Utilities;

namespace BotSharp.OpenAPI.Controllers;

public partial class ConversationController
{
    #region Files and attachments
    [HttpGet("/conversation/{conversationId}/attachments")]
    public List<MessageFileViewModel> ListAttachments([FromRoute] string conversationId)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var dir = fileStorage.GetDirectory(conversationId);

        // List files in the directory
        var files = Directory.Exists(dir)
            ? Directory.GetFiles(dir).Select(f => new MessageFileViewModel
            {
                FileName = Path.GetFileName(f),
                FileExtension = Path.GetExtension(f).TrimStart('.').ToLower(),
                ContentType = FileUtility.GetFileContentType(f),
                FileDownloadUrl = $"/conversation/{conversationId}/attachments/file/{Path.GetFileName(f)}",
            }).ToList()
            : new List<MessageFileViewModel>();

        return files;
    }

    [AllowAnonymous]
    [HttpGet("/conversation/{conversationId}/attachments/file/{fileName}")]
    public IActionResult GetAttachment([FromRoute] string conversationId, [FromRoute] string fileName)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var dir = fileStorage.GetDirectory(conversationId);
        var filePath = Path.Combine(dir, fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }
        return BuildFileResult(filePath);
    }

    [HttpPost("/conversation/{conversationId}/attachments")]
    public IActionResult UploadAttachments([FromRoute] string conversationId, IFormFile[] files)
    {
        if (files != null && files.Length > 0)
        {
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var dir = fileStorage.GetDirectory(conversationId);
            foreach (var file in files)
            {
                // Save the file, process it, etc.
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var filePath = Path.Combine(dir, fileName);

                fileStorage.SaveFileStreamToPath(filePath, file.OpenReadStream());
            }

            return Ok(new { message = "File uploaded successfully." });
        }

        return BadRequest(new { message = "Invalid file." });
    }

    [HttpPost("/agent/{agentId}/conversation/{conversationId}/upload")]
    public async Task<IActionResult> UploadConversationMessageFiles([FromRoute] string agentId, [FromRoute] string conversationId, [FromBody] InputMessageFiles input)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conversationId, input.States);
        var conv = await convService.GetConversationRecordOrCreateNew(agentId);
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var messageId = Guid.NewGuid().ToString();
        var isSaved = fileStorage.SaveMessageFiles(conv.Id, messageId, FileSource.User, input.Files);
        return Ok(new { messageId = isSaved ? messageId : string.Empty });
    }

    [HttpGet("/conversation/{conversationId}/files/{messageId}/{source}")]
    public IEnumerable<MessageFileViewModel> GetConversationMessageFiles([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string source)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var files = fileStorage.GetMessageFiles(conversationId, [messageId], options: new() { Sources = [source] });
        return files?.Select(x => MessageFileViewModel.Transform(x))?.ToList() ?? [];
    }

    [HttpGet("/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}")]
    public IActionResult GetMessageFile([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string source, [FromRoute] string index, [FromRoute] string fileName)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var file = fileStorage.GetMessageFile(conversationId, messageId, source, index, fileName);
        if (string.IsNullOrEmpty(file))
        {
            return NotFound();
        }
        return BuildFileResult(file);
    }

    [HttpGet("/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}/download")]
    public IActionResult DownloadMessageFile([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string source, [FromRoute] string index, [FromRoute] string fileName)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var file = fileStorage.GetMessageFile(conversationId, messageId, source, index, fileName);
        if (string.IsNullOrEmpty(file))
        {
            return NotFound();
        }

        var fName = file.Split(Path.DirectorySeparatorChar).Last();
        var contentType = FileUtility.GetFileContentType(fName);
        var stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        stream.Position = 0;

        return new FileStreamResult(stream, contentType) { FileDownloadName = fName };
    }
    #endregion
}
