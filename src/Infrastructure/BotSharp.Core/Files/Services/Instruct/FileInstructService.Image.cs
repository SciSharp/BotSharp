using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> ReadImages(string text, IEnumerable<InstructFileModel> images, InstructOptions? options = null)
    {
        var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
        var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

        var completion = CompletionProvider.GetChatCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "gpt-4o", multiModal: true);
        var message = await completion.GetChatCompletions(new Agent()
        {
            Id = innerAgentId,
            Instruction = instruction
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, text)
            {
                Files = images?.Select(x => new BotSharpFile
                {
                    FileUrl = x.FileUrl,
                    FileData = x.FileData,
                    ContentType = x.ContentType
                }).ToList() ?? []
            }
        });

        await HookEmitter.Emit<IInstructHook>(_services, async hook => 
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                TemplateName = options?.TemplateName,
                UserMessage = text,
                SystemInstruction = instruction,
                CompletionText = message.Content
            }), innerAgentId);

        return message.Content;
    }

    public async Task<RoleDialogModel> GenerateImage(string text, InstructOptions? options = null)
    {
        var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
        var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "dall-e-3");
        var message = await completion.GetImageGeneration(new Agent()
        {
            Id = innerAgentId,
            Instruction = instruction
        }, new RoleDialogModel(AgentRole.User, instruction ?? text));

        await HookEmitter.Emit<IInstructHook>(_services, async hook =>
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                TemplateName = options?.TemplateName,
                UserMessage = text,
                SystemInstruction = instruction,
                CompletionText = message.Content
            }), innerAgentId);

        return message;
    }

    public async Task<RoleDialogModel> VaryImage(InstructFileModel image, InstructOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData))
        {
            throw new ArgumentException($"Cannot find image url or data!");
        }

        var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "dall-e-2");
        var binary = await DownloadFile(image);
        using var stream = binary.ToStream();
        stream.Position = 0;

        var fileName = BuildFileName(image.FileName, image.FileExtension, "image", "png");
        var message = await completion.GetImageVariation(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, string.Empty), stream, fileName);

        stream.Close();

        await HookEmitter.Emit<IInstructHook>(_services, async hook =>
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                UserMessage = string.Empty,
                CompletionText = message.Content
            }), innerAgentId);

        return message;
    }

    public async Task<RoleDialogModel> EditImage(string text, InstructFileModel image, InstructOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData))
        {
            throw new ArgumentException($"Cannot find image url or data!");
        }

        var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
        var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "dall-e-2");
        var binary = await DownloadFile(image);
        using var stream = binary.ToStream();
        stream.Position = 0;

        var fileName = BuildFileName(image.FileName, image.FileExtension, "image", "png");
        var message = await completion.GetImageEdits(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, instruction ?? text), stream, fileName);

        stream.Close();

        await HookEmitter.Emit<IInstructHook>(_services, async hook =>
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                TemplateName = options?.TemplateName,
                UserMessage = text,
                SystemInstruction = instruction,
                CompletionText = message.Content
            }), innerAgentId);

        return message;
    }

    public async Task<RoleDialogModel> EditImage(string text, InstructFileModel image, InstructFileModel mask, InstructOptions? options = null)
    {
        if ((string.IsNullOrWhiteSpace(image?.FileUrl) && string.IsNullOrWhiteSpace(image?.FileData)) ||
            (string.IsNullOrWhiteSpace(mask?.FileUrl) && string.IsNullOrWhiteSpace(mask?.FileData)))
        {
            throw new ArgumentException($"Cannot find image/mask url or data");
        }

        var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
        var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "dall-e-2");
        var imageBinary = await DownloadFile(image);
        var maskBinary = await DownloadFile(mask);

        using var imageStream = imageBinary.ToStream();
        imageStream.Position = 0;

        using var maskStream = maskBinary.ToStream();
        maskStream.Position = 0;

        var imageName = BuildFileName(image.FileName, image.FileExtension, "image", "png");
        var maskName = BuildFileName(image.FileName, image.FileExtension, "mask", "png");
        var message = await completion.GetImageEdits(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, instruction ?? text), imageStream, imageName, maskStream, maskName);

        imageStream.Close();
        maskStream.Close();

        await HookEmitter.Emit<IInstructHook>(_services, async hook =>
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = innerAgentId,
                Provider = completion.Provider,
                Model = completion.Model,
                TemplateName = options?.TemplateName,
                UserMessage = text,
                SystemInstruction = instruction,
                CompletionText = message.Content
            }), innerAgentId);

        return message;
    }
}
