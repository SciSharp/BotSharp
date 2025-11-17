using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Options;
using System.IO;

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

        var textContent = text.IfNullOrEmptyAs(instruction).IfNullOrEmptyAs(string.Empty);
        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "gpt-image-1-mini");
        var message = await completion.GetImageGeneration(new Agent()
        {
            Id = innerAgentId,
            Instruction = instruction
        }, new RoleDialogModel(AgentRole.User, textContent));

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

        // Convert image
        var converter = GetImageConverter(options?.ImageConvertProvider);
        if (converter != null)
        {
            binary = await converter.ConvertImage(binary);
            image.FileExtension = "png";
        }

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

        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "gpt-image-1-mini");
        var binary = await DownloadFile(image);

        // Convert image
        var converter = GetImageConverter(options?.ImageConvertProvider);
        if (converter != null)
        {
            binary = await converter.ConvertImage(binary);
            image.FileExtension = "png";
        }

        using var stream = binary.ToStream();
        stream.Position = 0;

        var fileName = BuildFileName(image.FileName,image.FileExtension, "image", "png");
        var textContent = text.IfNullOrEmptyAs(instruction).IfNullOrEmptyAs(string.Empty);
        var message = await completion.GetImageEdits(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, textContent), stream, fileName);

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

        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "gpt-image-1-mini");
        var imageBinary = await DownloadFile(image);
        var maskBinary = await DownloadFile(mask);

        // Convert image
        var converter = GetImageConverter(options?.ImageConvertProvider);
        if (converter != null)
        {
            imageBinary = await converter.ConvertImage(imageBinary);
            image.FileExtension = "png";

            maskBinary = await converter.ConvertImage(maskBinary);
            mask.FileExtension = "png";
        }

        using var imageStream = imageBinary.ToStream();
        imageStream.Position = 0;

        using var maskStream = maskBinary.ToStream();
        maskStream.Position = 0;

        var imageName = BuildFileName(image.FileName, image.FileExtension, "image", "png");
        var maskName = BuildFileName(mask.FileName, mask.FileExtension, "mask", "png");
        var textContent = text.IfNullOrEmptyAs(instruction).IfNullOrEmptyAs(string.Empty);
        var message = await completion.GetImageEdits(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, textContent), imageStream, imageName, maskStream, maskName);

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

    public async Task<RoleDialogModel> ComposeImages(string text, InstructFileModel[] images, InstructOptions? options = null)
    {
        var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
        var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

        var completion = CompletionProvider.GetImageCompletion(_services, provider: options?.Provider ?? "openai", model: options?.Model ?? "gpt-image-1-mini");

        var streams = new List<Stream>();
        var fileNames = new List<string>();
        foreach (var image in images)
        {
            var binary = await DownloadFile(image);

            // Convert image
            var converter = GetImageConverter(options?.ImageConvertProvider);
            if (converter != null)
            {
                binary = await converter.ConvertImage(binary);
                image.FileExtension = "png";
            }

            var stream = binary.ToStream();
            streams.Add(stream);

            var fileName = BuildFileName(image.FileName, image.FileExtension, "image", "png");
            fileNames.Add(fileName);
        }

        var textContent = text.IfNullOrEmptyAs(instruction).IfNullOrEmptyAs(string.Empty);
        var message = await completion.GetImageComposition(new Agent()
        {
            Id = innerAgentId
        }, new RoleDialogModel(AgentRole.User, textContent), streams.ToArray(), fileNames.ToArray());

        foreach (var stream in streams)
        {
            stream.Close();
        }

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
