using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class BotSharpFileService
{
    public async Task<RoleDialogModel> GenerateImage(string? provider, string? model, string text)
    {
        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-3");
        var message = await completion.GetImageGeneration(new Agent()
        {
            Id = Guid.Empty.ToString(),
        }, new RoleDialogModel(AgentRole.User, text));
        return message;
    }

    public async Task<RoleDialogModel> VaryImage(string? provider, string? model, BotSharpFile image)
    {
        if (string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData))
        {
            throw new ArgumentException($"Cannot find image url or data!");
        }

        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var bytes = await DownloadFile(image);
        using var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        var message = await completion.GetImageVariation(new Agent()
        {
            Id = Guid.Empty.ToString()
        }, new RoleDialogModel(AgentRole.User, string.Empty), stream, image.FileName ?? string.Empty);
        
        stream.Close();
        return message;
    }

    public async Task<RoleDialogModel> EditImage(string? provider, string? model, string text, BotSharpFile image)
    {
        if (string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData))
        {
            throw new ArgumentException($"Cannot find image url or data!");
        }

        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var bytes = await DownloadFile(image);
        using var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        var message = await completion.GetImageEdits(new Agent()
        {
            Id = Guid.Empty.ToString()
        }, new RoleDialogModel(AgentRole.User, text), stream, image.FileName ?? string.Empty);
        
        stream.Close();
        return message;
    }

    public async Task<RoleDialogModel> EditImage(string? provider, string? model, string text, BotSharpFile image, BotSharpFile mask)
    {
        if ((string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData)) ||
            (string.IsNullOrWhiteSpace(mask?.FileUrl) && string.IsNullOrWhiteSpace(mask?.FileData)))
        {
            throw new ArgumentException($"Cannot find image/mask url or data");
        }

        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var imageBytes = await DownloadFile(image);
        var maskBytes = await DownloadFile(mask);

        using var imageStream = new MemoryStream();
        imageStream.Write(imageBytes, 0, imageBytes.Length);
        imageStream.Position = 0;

        using var maskStream = new MemoryStream();
        maskStream.Write(maskBytes, 0, maskBytes.Length);
        maskStream.Position = 0;

        var message = await completion.GetImageEdits(new Agent()
        {
            Id = Guid.Empty.ToString()
        }, new RoleDialogModel(AgentRole.User, text), imageStream, image.FileName ?? string.Empty, maskStream, mask.FileName ?? string.Empty);
        
        imageStream.Close();
        maskStream.Close();
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
