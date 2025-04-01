using BotSharp.Abstraction.Instructs.Models;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> SpeechToText(InstructFileModel audio, string? text = null, InstructOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
            text = await GetAgentTemplate(innerAgentId, options?.TemplateName);
        }

        var completion = CompletionProvider.GetAudioTranscriber(_services, provider: options?.Provider, model: options?.Model);
        var audioBytes = await DownloadFile(audio);
        using var stream = new MemoryStream();
        stream.Write(audioBytes, 0, audioBytes.Length);
        stream.Position = 0;

        var fileName = $"{audio.FileName ?? "audio"}.{audio.FileExtension ?? "wav"}";
        var content = await completion.TranscriptTextAsync(stream, fileName, text ?? string.Empty);
        stream.Close();
        return content;
    }
}
