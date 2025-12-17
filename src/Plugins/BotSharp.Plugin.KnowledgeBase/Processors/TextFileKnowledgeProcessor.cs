using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Knowledges.Helpers;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Processors;
using BotSharp.Abstraction.Knowledges.Responses;
using System.Net.Mime;

namespace BotSharp.Plugin.KnowledgeBase.Processors;

public class TextFileKnowledgeProcessor : IKnowledgeProcessor
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TextFileKnowledgeProcessor> _logger;

    public TextFileKnowledgeProcessor(
        IServiceProvider services,
        ILogger<TextFileKnowledgeProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "botsharp-txt-knowledge";

    public async Task<FileKnowledgeResponse> GetFileKnowledgeAsync(FileBinaryDataModel file, FileKnowledgeHandleOptions? options = null)
    {
        if (!file.ContentType.IsEqualTo(MediaTypeNames.Text.Plain))
        {
            return new();
        }

        var binary = file.FileBinaryData;
        using var stream = binary.ToStream();
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        reader.Close();
        stream.Close();

        var lines = TextChopper.Chop(content, ChunkOption.Default());
        return new FileKnowledgeResponse
        {
            Success = true,
            Knowledges = new List<FileKnowledgeModel>
            {
                new() { Contents = lines }
            }
        };
    }
}
