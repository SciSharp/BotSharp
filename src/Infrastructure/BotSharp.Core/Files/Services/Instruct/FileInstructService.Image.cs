using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> ReadImages(string? provider, string? model, string text, IEnumerable<InstructFileModel> images, string? agentId = null)
    {
        var innerAgentId = agentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetChatCompletion(_services, provider: provider ?? "openai", model: model ?? "gpt-4o", multiModal: true);
        var message = await completion.GetChatCompletions(new Agent()
        {
            Id = innerAgentId,
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, text)
            {
                Files = images?.Select(x => new BotSharpFile { FileUrl = x.FileUrl, FileData = x.FileData }).ToList() ?? new List<BotSharpFile>()
            }
        });

        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                UserMessage = text,
                CompletionText = message.Content
            });
        }

        return message.Content;
    }

    public async Task<RoleDialogModel> GenerateImage(string? provider, string? model, string text, string? agentId = null)
    {
        var innerAgentId = agentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-3");
        var message = await completion.GetImageGeneration(new Agent()
        {
            Id = innerAgentId,
        }, new RoleDialogModel(AgentRole.User, text));

        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                UserMessage = text,
                CompletionText = message.Content
            });
        }

        return message;
    }

    public async Task<RoleDialogModel> VaryImage(string? provider, string? model, InstructFileModel image, string? agentId = null)
    {
        if (string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData))
        {
            throw new ArgumentException($"Cannot find image url or data!");
        }

        var innerAgentId = agentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var bytes = await DownloadFile(image);
        using var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        var fileName = $"{image.FileName ?? "image"}.{image.FileExtension ?? "png"}";
        var message = await completion.GetImageVariation(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, string.Empty), stream, fileName);

        stream.Close();

        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                UserMessage = string.Empty,
                CompletionText = message.Content
            });
        }

        return message;
    }

    public async Task<RoleDialogModel> EditImage(string? provider, string? model, string text, InstructFileModel image, string? agentId = null)
    {
        if (string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData))
        {
            throw new ArgumentException($"Cannot find image url or data!");
        }

        var innerAgentId = agentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var bytes = await DownloadFile(image);
        using var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        var fileName = $"{image.FileName ?? "image"}.{image.FileExtension ?? "png"}";
        var message = await completion.GetImageEdits(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, text), stream, fileName);

        stream.Close();

        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                UserMessage = text,
                CompletionText = message.Content
            });
        }

        return message;
    }

    public async Task<RoleDialogModel> EditImage(string? provider, string? model, string text, InstructFileModel image, InstructFileModel mask, string? agentId = null)
    {
        if ((string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData)) ||
            (string.IsNullOrWhiteSpace(mask?.FileUrl) && string.IsNullOrWhiteSpace(mask?.FileData)))
        {
            throw new ArgumentException($"Cannot find image/mask url or data");
        }

        var innerAgentId = agentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
        var imageBytes = await DownloadFile(image);
        var maskBytes = await DownloadFile(mask);

        using var imageStream = new MemoryStream();
        imageStream.Write(imageBytes, 0, imageBytes.Length);
        imageStream.Position = 0;

        using var maskStream = new MemoryStream();
        maskStream.Write(maskBytes, 0, maskBytes.Length);
        maskStream.Position = 0;

        var imageName = $"{image.FileName ?? "image"}.{image.FileExtension ?? "png"}";
        var maskName = $"{mask.FileName ?? "mask"}.{mask.FileExtension ?? "png"}";
        var message = await completion.GetImageEdits(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, text), imageStream, imageName, maskStream, maskName);

        imageStream.Close();
        maskStream.Close();

        var hooks = _services.GetServices<IInstructHook>();
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != agentId)
            {
                continue;
            }

            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                UserMessage = text,
                CompletionText = message.Content
            });
        }

        return message;
    }

    #region Private methods
    private async Task<byte[]> DownloadFile(InstructFileModel file)
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
            (_, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
        }

        return bytes;
    }
    #endregion
}
