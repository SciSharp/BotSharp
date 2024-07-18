using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class BotSharpFileService
{
    public async Task<RoleDialogModel> GenerateImage(string? provider, string? model, string text)
    {
        var completion = CompletionProvider.GetImageGeneration(_services, provider: provider ?? "openai", model: model ?? "dall-e-3");
        var message = await completion.GetImageGeneration(new Agent()
        {
            Id = Guid.Empty.ToString(),
        }, new RoleDialogModel(AgentRole.User, text));
        return message;
    }

    public async Task<RoleDialogModel> VarifyImage(string? provider, string? model, BotSharpFile file)
    {
        if (string.IsNullOrWhiteSpace(file?.FileUrl) && string.IsNullOrWhiteSpace(file?.FileData))
        {
            throw new ArgumentException($"Please fill in at least file url or file data!");
        }

        var completion = CompletionProvider.GetImageVariation(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var bytes = await DownloadFile(file);
        using var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        var message = await completion.GetImageVariation(new Agent()
        {
            Id = Guid.Empty.ToString()
        }, new RoleDialogModel(AgentRole.User, string.Empty), stream, file.FileName ?? string.Empty);
        stream.Close();

        return message;
    }

    #region Private methods
    private async Task<byte[]> DownloadFile(BotSharpFile file)
    {
        var bytes = new byte[0];
        if (!string.IsNullOrEmpty(file.FileUrl))
        {
            var http = _services.GetRequiredService<IHttpClientFactory>();
            using var client = http.CreateClient();
            bytes = await client.GetByteArrayAsync(file.FileUrl);
        }
        else if (!string.IsNullOrEmpty(file.FileData))
        {
            (_, bytes) = GetFileInfoFromData(file.FileData);
        }

        return bytes;
    }
    #endregion
}
